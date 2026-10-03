using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [Header("Pool Settings")]
    public GameObject projectilePrefab;
    public int poolSize = 20;

    readonly Queue<Projectile> _pool = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(projectilePrefab, transform);
            go.SetActive(false);
            _pool.Enqueue(go.GetComponent<Projectile>());
        }
    }

    public Projectile GetProjectile()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();
        // Expand pool if exhausted
        var go = Instantiate(projectilePrefab, transform);
        return go.GetComponent<Projectile>();
    }

    public void ReturnProjectile(Projectile p)
    {
        p.gameObject.SetActive(false);
        _pool.Enqueue(p);
    }
}
