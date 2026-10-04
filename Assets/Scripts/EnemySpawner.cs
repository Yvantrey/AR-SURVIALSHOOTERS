using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public EnemyBase meleeEnemyPrefab;
    public EnemyBase shooterEnemyPrefab;

    [Header("Spawn Settings")]
    public float spawnInterval = 3f;
    [Tooltip("Enemies appear on a ring around the player between these distances (arena units).")]
    public float minSpawnDistance = 4f;
    public float maxSpawnDistance = 8f;
    public int maxAliveEnemies = 8;
    [HideInInspector] public int healthMultiplier = 1;
    // Kept for older scenes that serialized it.
    [HideInInspector] public float spawnRadius = 1.5f;

    bool _spawning;
    Coroutine _spawnRoutine;
    int _spawnCount;

    public void StartSpawning()
    {
        StopSpawning();
        _spawning = true;
        _spawnCount = 0;
        _spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        _spawning = false;
        if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
        _spawnRoutine = null;
    }

    IEnumerator SpawnLoop()
    {
        // A short grace period, then a couple of enemies right away so the arena never feels empty.
        yield return new WaitForSeconds(1.5f);
        SpawnEnemy();
        while (_spawning)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (GameManager.Instance == null || GameManager.Instance.ActiveEnemyCount < maxAliveEnemies) SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        var player = PlayerController.Instance;
        Transform arena = player != null && player.ArenaRoot != null ? player.ArenaRoot : transform;
        float scale = Mathf.Max(0.01f, arena.lossyScale.y);
        Bounds bounds = player != null ? player.ArenaLocalBounds : new Bounds(Vector3.zero, Vector3.zero);

        Vector3 playerLocal = player != null ? arena.InverseTransformPoint(player.Body.position) : Vector3.zero;
        Vector2 dir = Random.insideUnitCircle.normalized;
        if (dir == Vector2.zero) dir = Vector2.up;
        Vector3 local = playerLocal + new Vector3(dir.x, 0f, dir.y) * Random.Range(minSpawnDistance, maxSpawnDistance);
        if (bounds.size.x > 0.01f)
        {
            local.x = Mathf.Clamp(local.x, bounds.min.x, bounds.max.x);
            local.z = Mathf.Clamp(local.z, bounds.min.z, bounds.max.z);
        }
        local.y = 0f;
        Vector3 spawnPos = arena.TransformPoint(local);

        // Alternate between melee and shooter so both attack types show up.
        _spawnCount++;
        var prefab = shooterEnemyPrefab != null && meleeEnemyPrefab != null
            ? (_spawnCount % 2 == 0 ? shooterEnemyPrefab : meleeEnemyPrefab)
            : (meleeEnemyPrefab != null ? meleeEnemyPrefab : shooterEnemyPrefab);
        if (prefab == null) return;

        Vector3 facing = (player != null ? player.Body.position : arena.position) - spawnPos;
        facing.y = 0f;
        Quaternion rotation = facing.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(facing) : arena.rotation;
        EnemyBase enemy = Instantiate(prefab, spawnPos, rotation, transform);
        if (enemy == null) return;
        enemy.SetHealthMultiplier(healthMultiplier);
        enemy.SetWorldScale(scale);
        NormalizeHeight(enemy, scale);
    }

    /// <summary>The enemy models were imported at very different scales; make every enemy roughly human-sized.</summary>
    static void NormalizeHeight(EnemyBase enemy, float scale)
    {
        var renderers = enemy.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        if (b.size.y < 0.0001f) return;
        enemy.transform.localScale *= enemy.height * scale / b.size.y;
        b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        // Spawn position is on the floor; lift/drop the model so its feet rest there.
        enemy.transform.position += Vector3.up * (enemy.transform.position.y - b.min.y);
    }
}
