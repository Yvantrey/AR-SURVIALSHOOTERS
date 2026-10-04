using System.Collections.Generic;
using UnityEngine;

public enum GameState { Start, Placing, Playing, End }
public enum GameDifficulty { Easy, Hard }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public float gameDuration = 60f;
    public float easySpawnInterval = 3.5f;
    public float hardSpawnInterval = 1.8f;
    public int easyEnemyHealthMultiplier = 1;
    public int hardEnemyHealthMultiplier = 2;
    public int enemiesToDefeatToWin = 12;

    [Header("References")]
    public EnemySpawner enemySpawner;

    public GameState State { get; private set; } = GameState.Start;
    public int Score { get; private set; }
    public int EnemiesDefeated { get; private set; }
    public float TimeRemaining { get; private set; }
    public GameDifficulty Difficulty { get; private set; } = GameDifficulty.Easy;
    public bool Won { get; private set; }
    public int ActiveEnemyCount => _activeEnemies.Count;
    public IReadOnlyList<EnemyBase> ActiveEnemies => _activeEnemies;

    readonly List<EnemyBase> _activeEnemies = new();
    int _lastHudSecond = -1;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (State != GameState.Playing) return;
        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0f) { TimeRemaining = 0f; EndGame(true); }
        else if (Mathf.CeilToInt(TimeRemaining) != _lastHudSecond)
        {
            _lastHudSecond = Mathf.CeilToInt(TimeRemaining);
            UIManager.Instance?.UpdateHUD();
        }
    }

    ARPlacementController Placement => FindObjectOfType<ARPlacementController>();

    public void StartGame() => BeginPlacement(GameDifficulty.Easy);

    /// <summary>Start button: anchor the barn to the scanned plane (auto at screen centre, otherwise by tap), then play.</summary>
    public void BeginPlacement(GameDifficulty difficulty)
    {
        if (State == GameState.Playing) return;
        Difficulty = difficulty;
        ResetRound();
        State = GameState.Placing;
        UIManager.Instance?.ShowPlacementPrompt();
        Placement?.BeginPlacement();
    }

    public void OnWorldPlaced()
    {
        if (State != GameState.Placing) return;
        TimeRemaining = gameDuration;
        _lastHudSecond = Mathf.CeilToInt(TimeRemaining);
        State = GameState.Playing;
        var placement = Placement;
        PlayerController.Instance?.ConfigureArenaCamera(placement != null && placement.gameWorldRoot != null ? placement.gameWorldRoot.transform : null);
        if (enemySpawner != null)
        {
            enemySpawner.spawnInterval = Difficulty == GameDifficulty.Hard ? hardSpawnInterval : easySpawnInterval;
            enemySpawner.healthMultiplier = Difficulty == GameDifficulty.Hard ? hardEnemyHealthMultiplier : easyEnemyHealthMultiplier;
            enemySpawner.StartSpawning();
        }
        UIManager.Instance?.ShowHUD();
    }

    public void AddScore(int points)
    {
        if (State != GameState.Playing) return;
        Score += points;
        EnemiesDefeated++;
        UIManager.Instance?.UpdateHUD();
        if (enemiesToDefeatToWin > 0 && EnemiesDefeated >= enemiesToDefeatToWin) EndGame(true);
    }

    public void RegisterEnemy(EnemyBase e) { if (!_activeEnemies.Contains(e)) _activeEnemies.Add(e); }
    public void UnregisterEnemy(EnemyBase e) => _activeEnemies.Remove(e);

    public void TriggerGameOver() => EndGame(false);

    void EndGame(bool won)
    {
        if (State == GameState.End) return;
        Won = won;
        State = GameState.End;
        ClearBattlefield();
        PlayerController.Instance?.SetFiring(false);
        PlayerController.Instance?.SetMoveInput(Vector2.zero);
        float survived = Mathf.Clamp(gameDuration - TimeRemaining, 0f, gameDuration);
        LeaderboardManager.Instance?.SaveSession(Score, EnemiesDefeated, survived, won, Difficulty.ToString());
        UIManager.Instance?.ShowEndScreen();
    }

    /// <summary>Restart: play again straight away on the barn that is already placed.</summary>
    public void RestartGame()
    {
        ClearBattlefield();
        ResetRound(); // keeps the current difficulty
        State = GameState.Placing;
        var placement = Placement;
        if (placement != null && placement.HasPlacedWorld) placement.ReuseCurrentPlacement();
        else
        {
            UIManager.Instance?.ShowPlacementPrompt();
            placement?.BeginPlacement();
        }
    }

    /// <summary>Menu: hide the barn, bring the plane detector back and show the start menu.</summary>
    public void ReturnToMenu()
    {
        ClearBattlefield();
        PlayerController.Instance?.ResetForNewGame();
        State = GameState.Start;
        var placement = Placement;
        if (placement != null) placement.ResetPlacement();
        if (placement != null && placement.SurfaceReady) UIManager.Instance?.ShowStartMenu();
        else UIManager.Instance?.ShowScanningPrompt();
    }

    void ResetRound()
    {
        Score = 0;
        EnemiesDefeated = 0;
        Won = false;
        TimeRemaining = gameDuration;
        _lastHudSecond = Mathf.CeilToInt(TimeRemaining);
        PlayerController.Instance?.ResetForNewGame();
        enemySpawner?.StopSpawning();
    }

    void ClearBattlefield()
    {
        enemySpawner?.StopSpawning();
        foreach (var e in new List<EnemyBase>(_activeEnemies))
            if (e != null) Destroy(e.gameObject);
        _activeEnemies.Clear();
        if (enemySpawner != null)
            foreach (var e in enemySpawner.GetComponentsInChildren<EnemyBase>(true)) Destroy(e.gameObject);
        ObjectPool.Instance?.ReturnAll();
    }

    public EnemyBase GetClosestEnemy(Vector3 from, float maxDistance)
    {
        EnemyBase closest = null;
        float closestDistance = maxDistance;
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            EnemyBase enemy = _activeEnemies[i];
            if (enemy == null) { _activeEnemies.RemoveAt(i); continue; }
            if (enemy.IsDead) continue;
            float distance = Vector3.Distance(from, enemy.transform.position);
            if (distance >= closestDistance) continue;
            closestDistance = distance;
            closest = enemy;
        }
        return closest;
    }
}
