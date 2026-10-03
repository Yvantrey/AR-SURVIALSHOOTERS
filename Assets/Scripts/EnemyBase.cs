using System.Collections;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 2;
    public int scoreValue = 10;

    [Header("Movement")]
    public float moveSpeed = 2f;

    protected int _currentHealth;
    protected Transform _player;
    protected bool _isDead;

    protected virtual void Awake() { }

    protected virtual void Start()
    {
        _currentHealth = maxHealth;
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
        GameManager.Instance?.RegisterEnemy(this);
        AudioManager.Instance?.PlayEnemySpawn(transform.position);
    }

    protected virtual void Update()
    {
        if (_isDead || _player == null) return;
        HandleBehavior();
    }

    protected abstract void HandleBehavior();

    public virtual void TakeDamage(int amount)
    {
        if (_isDead) return;
        _currentHealth -= amount;
        StartCoroutine(HitFlash());
        if (_currentHealth <= 0) Die();
    }

    IEnumerator HitFlash()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            foreach (var m in r.materials) m.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        foreach (var r in renderers)
            foreach (var m in r.materials) m.color = Color.white;
    }

    protected void MoveToward(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    protected virtual void Die()
    {
        _isDead = true;
        GameManager.Instance?.AddScore(scoreValue);
        GameManager.Instance?.UnregisterEnemy(this);
        Destroy(gameObject, 0.3f);
    }
}
