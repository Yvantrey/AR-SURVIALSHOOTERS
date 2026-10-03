using UnityEngine;

public class ShooterEnemy : EnemyBase
{
    [Header("Shooter")]
    public float shootRange = 5f;
    public float shootCooldown = 2f;
    public GameObject firePoint;

    float _shootTimer;

    protected override void Awake()
    {
        maxHealth = 4; // takes 4 bullets
        base.Awake();
    }

    protected override void HandleBehavior()
    {
        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist > shootRange)
        {
            MoveToward(_player.position);
        }
        else
        {
            transform.LookAt(_player);
            _shootTimer -= Time.deltaTime;
            if (_shootTimer <= 0f)
            {
                _shootTimer = shootCooldown;
                Shoot();
            }
        }
    }

    void Shoot()
    {
        Transform spawnPoint = firePoint != null ? firePoint.transform : transform;
        var proj = ObjectPool.Instance?.GetProjectile();
        if (proj == null) return;
        proj.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        proj.Init(isEnemyProjectile: true);
        AudioManager.Instance?.PlayEnemyShoot(transform.position);
    }
}
