using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 2;
    public int scoreValue = 10;

    [Header("Movement")]
    public float moveSpeed = 1.6f;
    public float height = 1.7f;

    protected int _currentHealth;
    protected Transform _player;
    protected bool _isDead;
    int _healthMultiplier = 1;
    float _worldScale = 1f;
    readonly Dictionary<Material, Color> _originalColors = new();

    public bool IsDead => _isDead;
    /// <summary>Uniform scale of the arena; distances and speeds are authored for scale 1.</summary>
    protected float WorldScale => _worldScale;
    /// <summary>Chest height, used by the player's auto-aim.</summary>
    public Vector3 AimPoint => transform.position + transform.up * (height * 0.55f * _worldScale);

    public void SetHealthMultiplier(int multiplier)
    {
        _healthMultiplier = Mathf.Max(1, multiplier);
    }

    public void SetWorldScale(float scale) => _worldScale = Mathf.Max(0.01f, scale);

    protected virtual void Awake() { }

    protected virtual void Start()
    {
        _currentHealth = maxHealth * _healthMultiplier;
        foreach (var r in GetComponentsInChildren<Renderer>())
            foreach (var m in r.materials)
                if (m.HasProperty("_BaseColor") || m.HasProperty("_Color")) _originalColors[m] = m.color;
        GameManager.Instance?.RegisterEnemy(this);
        AudioManager.Instance?.PlayEnemySpawn(transform.position);
    }

    protected virtual void Update()
    {
        if (_isDead || GameManager.Instance?.State != GameState.Playing) return;
        // Target the player's avatar on the barn floor (or the camera before an avatar exists).
        _player = PlayerController.Instance != null ? PlayerController.Instance.Body : (Camera.main != null ? Camera.main.transform : null);
        if (_player == null) return;
        HandleBehavior();
    }

    protected abstract void HandleBehavior();

    protected float FlatDistanceToPlayer()
    {
        Vector3 target = _player.position;
        target.y = transform.position.y;
        return Vector3.Distance(transform.position, target);
    }

    public virtual void TakeDamage(int amount)
    {
        if (_isDead) return;
        _currentHealth -= amount;
        StartCoroutine(HitFlash());
        if (_currentHealth <= 0) Die();
    }

    IEnumerator HitFlash()
    {
        foreach (var m in _originalColors.Keys) if (m != null) m.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        foreach (var pair in _originalColors) if (pair.Key != null) pair.Key.color = pair.Value;
    }

    protected void MoveToward(Vector3 target)
    {
        target.y = transform.position.y;
        Vector3 dir = (target - transform.position).normalized;
        transform.position += dir * (moveSpeed * _worldScale * Time.deltaTime);
        FaceDirection(dir);
    }

    protected void FaceDirection(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }

    protected virtual void Die()
    {
        _isDead = true;
        GameManager.Instance?.UnregisterEnemy(this);
        GameManager.Instance?.AddScore(scoreValue);
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        StartCoroutine(SinkAndDestroy());
    }

    IEnumerator SinkAndDestroy()
    {
        Vector3 start = transform.localScale;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(start, start * 0.1f, t / 0.3f);
            yield return null;
        }
        Destroy(gameObject);
    }
}
