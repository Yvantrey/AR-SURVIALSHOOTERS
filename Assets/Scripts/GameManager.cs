using System.Collections.Generic;
using UnityEngine;

public enum GameState { Start, Playing, End }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public float gameDuration = 60f;

    [Header("References")]
    public EnemySpawner enemySpawner;

    public GameState State { get; private set; } = GameState.Start;
    public int Score { get; private set; }
    public int EnemiesDefeated { get; private set; }
    public float TimeRemaining { get; private set; }

    readonly List<EnemyBase> _activeEnemies = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (State != GameState.Playing) return;
        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0f) EndGame();
    }

    public void StartGame()
    {
        Score = 0;
        EnemiesDefeated = 0;
        TimeRemaining = gameDuration;
        State = GameState.Playing;
        enemySpawner?.StartSpawning();
        UIManager.Instance?.ShowHUD();
    }

    public void AddScore(int points)
    {
        Score += points;
        EnemiesDefeated++;
        UIManager.Instance?.UpdateHUD();
    }

    public void RegisterEnemy(EnemyBase e) => _activeEnemies.Add(e);
    public void UnregisterEnemy(EnemyBase e) => _activeEnemies.Remove(e);

    public void TriggerGameOver() => EndGame();

    void EndGame()
    {
        State = GameState.End;
        enemySpawner?.StopSpawning();
        foreach (var e in new List<EnemyBase>(_activeEnemies))
            if (e != null) Destroy(e.gameObject);
        _activeEnemies.Clear();
        LeaderboardManager.Instance?.SaveSession(Score, EnemiesDefeated, gameDuration - TimeRemaining);
        UIManager.Instance?.ShowEndScreen();
    }

    public void RestartGame()
    {
        State = GameState.Start;
        UIManager.Instance?.ShowStartMenu();
    }
}
