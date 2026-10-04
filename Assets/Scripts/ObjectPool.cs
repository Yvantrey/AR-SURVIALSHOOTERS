using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [Header("Pool Settings")]
    public GameObject projectilePrefab;
    public int poolSize = 30;

    readonly Queue<Projectile> _pool = new();
    readonly List<Projectile> _all = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (projectilePrefab == null)
        {
            Debug.LogError("ObjectPool requires a Projectile prefab.", this);
            return;
        }
        for (int i = 0; i < poolSize; i++) _pool.Enqueue(Create());
    }

    Projectile Create()
    {
        var go = Instantiate(projectilePrefab, transform);
        go.SetActive(false);
        var projectile = go.GetComponent<Projectile>();
        if (projectile == null) projectile = go.AddComponent<Projectile>();
        _all.Add(projectile);
        return projectile;
    }

    public Projectile GetProjectile()
    {
        if (projectilePrefab == null) return null;
        // Expand pool if exhausted
        return _pool.Count > 0 ? _pool.Dequeue() : Create();
    }

    public void ReturnProjectile(Projectile p)
    {
        p.gameObject.SetActive(false);
        if (!_pool.Contains(p)) _pool.Enqueue(p);
    }

    public void ReturnAll()
    {
        foreach (var p in _all)
            if (p != null && p.gameObject.activeSelf) p.ReturnToPool();
    }
}
