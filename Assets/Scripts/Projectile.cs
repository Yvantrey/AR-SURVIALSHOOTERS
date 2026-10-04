using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 6f;
    public float lifetime = 4f;
    public int damage = 1;
    public float hitRadius = 0.12f;
    public Color playerBulletColor = new Color(1f, 0.85f, 0.2f);
    public Color enemyBulletColor = new Color(1f, 0.2f, 0.2f);

    bool _isEnemyProjectile;
    float _timer;
    float _currentSpeed;
    int _currentDamage;
    bool _active;
    Renderer _renderer;
    readonly RaycastHit[] _hits = new RaycastHit[8];

    void Awake() => _renderer = GetComponentInChildren<Renderer>();

    public void Init(bool isEnemyProjectile) => Init(isEnemyProjectile, damage, speed);

    public void Init(bool isEnemyProjectile, int hitDamage, float travelSpeed)
    {
        _isEnemyProjectile = isEnemyProjectile;
        _currentDamage = Mathf.Max(1, hitDamage);
        _currentSpeed = travelSpeed > 0f ? travelSpeed : speed;
        _timer = lifetime;
        _active = true;
        if (_renderer != null) _renderer.material.color = isEnemyProjectile ? enemyBulletColor : playerBulletColor;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!_active) return;
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) { ReturnToPool(); return; }

        float step = _currentSpeed * Time.deltaTime;
        // Sweep the path travelled this frame so fast bullets can't tunnel through thin enemies.
        int count = Physics.SphereCastNonAlloc(transform.position, hitRadius, transform.forward, _hits, step, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
            if (TryHit(_hits[i].collider)) return;

        transform.position += transform.forward * step;
        _timer -= Time.deltaTime;
        if (_timer <= 0f) ReturnToPool();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_active) TryHit(other);
    }

    bool TryHit(Collider other)
    {
        if (other == null) return false;
        if (_isEnemyProjectile)
        {
            if (!other.CompareTag("Player")) return false;
            PlayerController.Instance?.TakeDamage(_currentDamage);
        }
        else
        {
            var enemy = other.GetComponentInParent<EnemyBase>();
            if (enemy == null || enemy.IsDead) return false;
            enemy.TakeDamage(_currentDamage);
        }
        ReturnToPool();
        return true;
    }

    public void ReturnToPool()
    {
        if (!_active) return;
        _active = false;
        gameObject.SetActive(false);
        ObjectPool.Instance?.ReturnProjectile(this);
    }
}
