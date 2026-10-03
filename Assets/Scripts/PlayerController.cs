using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Stats")]
    public int maxHealth = 100;

    [Header("Shooting")]
    public Transform firePoint;
    public float fireRate = 0.3f;

    [Header("Feedback")]
    public Image damageOverlay; // red flash UI image

    int _currentHealth;
    float _fireTimer;
    bool _isDead;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _currentHealth = maxHealth;
        UIManager.Instance?.UpdateHUD();
    }

    void Update()
    {
        if (GameManager.Instance?.State != GameState.Playing) return;
        _fireTimer -= Time.deltaTime;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            TryShoot();
        // Editor testing fallback
        if (Input.GetMouseButtonDown(0))
            TryShoot();
    }

    void TryShoot()
    {
        if (_fireTimer > 0f) return;
        _fireTimer = fireRate;
        var proj = ObjectPool.Instance?.GetProjectile();
        if (proj == null) return;
        proj.transform.SetPositionAndRotation(firePoint.position, firePoint.rotation);
        proj.Init(isEnemyProjectile: false);
        AudioManager.Instance?.PlayPlayerShoot(transform.position);
    }

    public void TakeDamage(int amount)
    {
        if (_isDead) return;
        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        UIManager.Instance?.UpdateHUD();
        StartCoroutine(DamageFlash());
        if (_currentHealth <= 0) Die();
    }

    IEnumerator DamageFlash()
    {
        if (damageOverlay == null) yield break;
        damageOverlay.color = new Color(1, 0, 0, 0.4f);
        yield return new WaitForSeconds(0.2f);
        damageOverlay.color = new Color(1, 0, 0, 0f);
    }

    void Die()
    {
        _isDead = true;
        AudioManager.Instance?.PlayPlayerDeath(transform.position);
        GameManager.Instance?.TriggerGameOver();
    }

    public float HealthPercent => (float)_currentHealth / maxHealth;
}
