using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject startMenuPanel;
    public GameObject hudPanel;
    public GameObject endPanel;
    public GameObject leaderboardPanel;

    [Header("Start Menu")]
    public Button startButton;
    public Button leaderboardButton;

    [Header("HUD")]
    public Slider healthBar;
    public TMP_Text scoreText;
    public TMP_Text timerText;

    [Header("End Screen")]
    public TMP_Text finalScoreText;
    public TMP_Text enemiesDefeatedText;
    public TMP_Text timeSurvivedText;
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Leaderboard")]
    public TMP_Text leaderboardText;
    public Button leaderboardBackButton;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (startButton) startButton.onClick.AddListener(() => GameManager.Instance?.StartGame());
        if (restartButton) restartButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        if (mainMenuButton) mainMenuButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        if (leaderboardButton) leaderboardButton.onClick.AddListener(ShowLeaderboard);
        if (leaderboardBackButton) leaderboardBackButton.onClick.AddListener(ShowStartMenu);
        ShowStartMenu();
    }

    public void ShowStartMenu()
    {
        if (startMenuPanel) startMenuPanel.SetActive(true);
        if (hudPanel) hudPanel.SetActive(false);
        if (endPanel) endPanel.SetActive(false);
        if (leaderboardPanel) leaderboardPanel.SetActive(false);
    }

    public void ShowHUD()
    {
        if (startMenuPanel) startMenuPanel.SetActive(false);
        if (hudPanel) hudPanel.SetActive(true);
        if (endPanel) endPanel.SetActive(false);
        if (leaderboardPanel) leaderboardPanel.SetActive(false);
        UpdateHUD();
    }

    public void ShowEndScreen()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (finalScoreText) finalScoreText.text = $"Score: {gm.Score}";
        if (enemiesDefeatedText) enemiesDefeatedText.text = $"Enemies: {gm.EnemiesDefeated}";
        if (timeSurvivedText) timeSurvivedText.text = $"Time: {(gm.gameDuration - gm.TimeRemaining):F1}s";
        if (hudPanel) hudPanel.SetActive(false);
        if (endPanel) endPanel.SetActive(true);
    }

    public void UpdateHUD()
    {
        var gm = GameManager.Instance;
        var pc = PlayerController.Instance;
        if (pc != null && healthBar != null) healthBar.value = pc.HealthPercent;
        if (gm != null)
        {
            if (scoreText) scoreText.text = $"Score: {gm.Score}";
            if (timerText) timerText.text = $"{Mathf.CeilToInt(gm.TimeRemaining)}s";
        }
    }

    void ShowLeaderboard()
    {
        startMenuPanel.SetActive(false);
        leaderboardPanel.SetActive(true);
        var entries = LeaderboardManager.Instance?.LoadAll();
        if (entries == null || entries.Count == 0) { leaderboardText.text = "No sessions yet."; return; }
        var sb = new StringBuilder();
        for (int i = 0; i < entries.Count; i++)
            sb.AppendLine($"{i + 1}. Score:{entries[i].score}  Kills:{entries[i].enemiesDefeated}  Time:{entries[i].timeSurvived:F1}s");
        leaderboardText.text = sb.ToString();
    }
}
