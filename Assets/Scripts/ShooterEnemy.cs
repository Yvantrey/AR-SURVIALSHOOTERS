using UnityEngine;

public class ShooterEnemy : EnemyBase
{
    [Header("Shooter")]
    public float shootRange = 6f;
    public float shootCooldown = 1.8f;
    public int bulletDamage = 10;
    public float bulletSpeed = 7f;
    public GameObject firePoint;

    float _shootTimer = 1f;

    protected override void Awake()
    {
        maxHealth = 4; // takes 4 bullets
        base.Awake();
        // The prefab may reference the FirePoint *asset* instead of a child; only use a point that moves with this enemy.
        if (firePoint != null && !firePoint.transform.IsChildOf(transform)) firePoint = null;
    }

    protected override void HandleBehavior()
    {
        float dist = FlatDistanceToPlayer();

        if (dist > shootRange * WorldScale)
        {
            MoveToward(_player.position);
            return;
        }

        FaceDirection(_player.position - transform.position);
        _shootTimer -= Time.deltaTime;
        if (_shootTimer <= 0f)
        {
            _shootTimer = shootCooldown;
            Shoot();
        }
    }

    void Shoot()
    {
        var proj = ObjectPool.Instance?.GetProjectile();
        if (proj == null) return;
        Vector3 origin = firePoint != null
            ? firePoint.transform.position
            : transform.position + transform.up * (height * 0.6f * WorldScale) + transform.forward * (0.4f * WorldScale);
        Vector3 target = PlayerController.Instance != null ? PlayerController.Instance.AimPoint : _player.position;
        Vector3 direction = target - origin;
        if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;
        proj.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction.normalized));
        proj.Init(isEnemyProjectile: true, bulletDamage, bulletSpeed * WorldScale);
        AudioManager.Instance?.PlayEnemyShoot(origin);
    }
}
