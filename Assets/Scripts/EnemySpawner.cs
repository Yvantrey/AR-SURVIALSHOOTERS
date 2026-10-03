using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public EnemyBase meleeEnemyPrefab;
    public EnemyBase shooterEnemyPrefab;

    [Header("Spawn Settings")]
    public float spawnInterval = 3f;
    public float spawnRadius = 1.5f;

    bool _spawning;
    Coroutine _spawnRoutine;

    public void StartSpawning()
    {
        _spawning = true;
        _spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        _spawning = false;
        if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
    }

    IEnumerator SpawnLoop()
    {
        while (_spawning)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        Vector2 circle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(circle.x, 0f, circle.y);

        // Alternate between melee and shooter
        var prefab = (Random.value > 0.5f) ? shooterEnemyPrefab : meleeEnemyPrefab;
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }
}
