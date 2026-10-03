using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 6f;
    public float lifetime = 4f;
    public int damage = 1;

    bool _isEnemyProjectile;
    float _timer;

    public void Init(bool isEnemyProjectile)
    {
        _isEnemyProjectile = isEnemyProjectile;
        _timer = lifetime;
        gameObject.SetActive(true);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
        _timer -= Time.deltaTime;
        if (_timer <= 0f) ReturnToPool();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_isEnemyProjectile)
        {
            if (other.CompareTag("Player"))
            {
                PlayerController.Instance?.TakeDamage(damage);
                ReturnToPool();
            }
        }
        else
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                ReturnToPool();
            }
        }
    }

    void ReturnToPool()
    {
        gameObject.SetActive(false);
        ObjectPool.Instance?.ReturnProjectile(this);
    }
}
