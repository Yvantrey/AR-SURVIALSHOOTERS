using System.Collections;
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
    public Button hardButton;

    [Header("HUD")]
    public Slider healthBar;
    public TMP_Text healthText;
    public TMP_Text scoreText;
    public TMP_Text timerText;
    public TMP_Text livesText;
    public TMP_Text killsText;
    public TMP_Text messageText;
    public TMP_Text controlsHint;
    public Button turnLeftButton;
    public Button turnRightButton;

    [Header("End Screen")]
    public TMP_Text finalScoreText;
    public TMP_Text enemiesDefeatedText;
    public TMP_Text timeSurvivedText;
    public TMP_Text bestScoreText;
    public Button restartButton;
    public Button mainMenuButton;
    public Button endLeaderboardButton;
    public TMP_Text resultTitle;

    [Header("Leaderboard")]
    public TMP_Text leaderboardText;
    public Button leaderboardBackButton;
    public TMP_Text placementPrompt;
    public Button shootButton;
    public GameObject scanningPrompt;

    [Header("Camera Buttons")]
    public float cameraTurnSpeed = 90f;

    GameObject _leaderboardReturnPanel;
    Coroutine _messageRoutine;
    float _camYawInput;
    static Sprite _solidSprite;

    static readonly Color WinColor = new Color(0.35f, 1f, 0.45f);
    static readonly Color LoseColor = new Color(1f, 0.35f, 0.3f);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    void Start()
    {
        if (startButton) startButton.onClick.AddListener(() => GameManager.Instance?.BeginPlacement(GameDifficulty.Easy));
        if (hardButton) hardButton.onClick.AddListener(() => GameManager.Instance?.BeginPlacement(GameDifficulty.Hard));
        if (restartButton) restartButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        if (mainMenuButton) mainMenuButton.onClick.RemoveAllListeners();
        if (mainMenuButton) mainMenuButton.onClick.AddListener(() => GameManager.Instance?.ReturnToMenu());
        if (leaderboardButton) leaderboardButton.onClick.AddListener(() => ShowLeaderboard(startMenuPanel));
        if (endLeaderboardButton) endLeaderboardButton.onClick.AddListener(() => ShowLeaderboard(endPanel));
        if (leaderboardBackButton) leaderboardBackButton.onClick.AddListener(ShowLeaderboardReturnPanel);

        // Hold to fire / hold to rotate the camera.
        BindHold(shootButton, () => PlayerController.Instance?.SetFiring(true), () => PlayerController.Instance?.SetFiring(false));
        BindHold(turnLeftButton, () => _camYawInput = -1f, () => _camYawInput = 0f);
        BindHold(turnRightButton, () => _camYawInput = 1f, () => _camYawInput = 0f);
        ShowScanningPrompt();
    }

    void Update()
    {
        var pc = PlayerController.Instance;
        if (pc == null || GameManager.Instance?.State != GameState.Playing) return;
        if (_camYawInput != 0f) pc.RotateArenaCamera(_camYawInput * cameraTurnSpeed * Time.deltaTime);
    }

    static void BindHold(Button button, UnityEngine.Events.UnityAction press, UnityEngine.Events.UnityAction release)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        var hold = button.GetComponent<HoldButton>();
        if (hold == null) hold = button.gameObject.AddComponent<HoldButton>();
        hold.onPress.AddListener(press);
        hold.onRelease.AddListener(release);
    }

    // ---------- Screens ----------

    void ShowOnly(GameObject panel)
    {
        if (startMenuPanel) startMenuPanel.SetActive(panel == startMenuPanel);
        if (hudPanel) hudPanel.SetActive(panel == hudPanel);
        if (endPanel) endPanel.SetActive(panel == endPanel);
        if (leaderboardPanel) leaderboardPanel.SetActive(panel == leaderboardPanel);
        if (placementPrompt) placementPrompt.gameObject.SetActive(placementPrompt.gameObject == panel);
        if (scanningPrompt) scanningPrompt.SetActive(panel == scanningPrompt);
        if (panel != hudPanel)
        {
            _camYawInput = 0f;
            PlayerController.Instance?.SetFiring(false);
            PlayerController.Instance?.SetMoveInput(Vector2.zero);
        }
    }

    public void ShowStartMenu() => ShowOnly(startMenuPanel);

    public void ShowScanningPrompt() => ShowOnly(scanningPrompt);

    public void ShowPlacementPrompt()
    {
        if (placementPrompt) placementPrompt.text = "Point your phone at a detected surface (or tap it) to place the barn";
        ShowOnly(placementPrompt != null ? placementPrompt.gameObject : null);
    }

    public void ShowHUD()
    {
        ShowOnly(hudPanel);
        if (messageText) messageText.gameObject.SetActive(false);
        UpdateHUD();
        FlashMessage("SURVIVE!", 1.5f);
        if (controlsHint) StartCoroutine(HideHintLater(controlsHint, 6f));
    }

    public void ShowEndScreen()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (resultTitle)
        {
            resultTitle.text = gm.Won ? "YOU WIN!" : "YOU DIED";
            resultTitle.color = gm.Won ? WinColor : LoseColor;
        }
        if (finalScoreText) finalScoreText.text = $"Score: {gm.Score}";
        if (enemiesDefeatedText) enemiesDefeatedText.text = $"Enemies defeated: {gm.EnemiesDefeated}";
        if (timeSurvivedText) timeSurvivedText.text = $"Time survived: {Mathf.Clamp(gm.gameDuration - gm.TimeRemaining, 0f, gm.gameDuration):F1}s";
        if (bestScoreText)
        {
            var top = LeaderboardManager.Instance?.LoadTop(1);
            int best = top != null && top.Count > 0 ? top[0].score : gm.Score;
            bestScoreText.text = gm.Score >= best && gm.Score > 0 ? "NEW HIGH SCORE!" : $"High score: {best}";
        }
        ShowOnly(endPanel);
    }

    public void UpdateHUD()
    {
        var gm = GameManager.Instance;
        var pc = PlayerController.Instance;
        if (pc != null)
        {
            if (healthBar != null) healthBar.value = pc.HealthPercent;
            if (healthText != null) healthText.text = $"HEALTH {pc.CurrentHealth}/{pc.maxHealth}";
            if (livesText != null) livesText.text = $"LIVES {Mathf.Max(0, pc.CurrentLives)}/{pc.maxLives}";
        }
        if (gm != null)
        {
            if (scoreText) scoreText.text = $"SCORE {gm.Score}";
            if (timerText) timerText.text = $"TIME {Mathf.CeilToInt(Mathf.Max(0f, gm.TimeRemaining))}";
            if (killsText) killsText.text = gm.enemiesToDefeatToWin > 0 ? $"KILLS {gm.EnemiesDefeated}/{gm.enemiesToDefeatToWin}" : $"KILLS {gm.EnemiesDefeated}";
        }
    }

    public void FlashMessage(string message, float seconds = 1.2f)
    {
        if (messageText == null || !isActiveAndEnabled) return;
        if (_messageRoutine != null) StopCoroutine(_messageRoutine);
        _messageRoutine = StartCoroutine(MessageRoutine(message, seconds));
    }

    IEnumerator MessageRoutine(string message, float seconds)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(seconds);
        messageText.gameObject.SetActive(false);
    }

    static IEnumerator HideHintLater(TMP_Text hint, float seconds)
    {
        hint.gameObject.SetActive(true);
        yield return new WaitForSeconds(seconds);
        if (hint != null) hint.gameObject.SetActive(false);
    }

    void ShowLeaderboard(GameObject returnPanel)
    {
        if (leaderboardPanel == null || leaderboardText == null) return;
        _leaderboardReturnPanel = returnPanel;
        ShowOnly(leaderboardPanel);
        var entries = LeaderboardManager.Instance?.LoadTop(10);
        if (entries == null || entries.Count == 0) { leaderboardText.text = "No games played yet.\nPlay a round to set a score!"; return; }
        var last = LeaderboardManager.Instance.LastSession;
        var sb = new StringBuilder();
        sb.AppendLine("<b>#   SCORE   KILLS   TIME   RESULT</b>");
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            string result = e.won ? "<color=#5f5>WIN</color>" : "<color=#f55>LOSS</color>";
            string line = $"{i + 1,-3} {e.score,6}   {e.enemiesDefeated,5}   {e.timeSurvived,4:F0}s   {result}";
            bool isLatest = last != null && e.date == last.date && e.score == last.score && e.timeSurvived == last.timeSurvived;
            sb.AppendLine(isLatest ? $"<color=#ffd84d>{line}  < YOU</color>" : line);
        }
        leaderboardText.text = "<mspace=0.6em>" + sb + "</mspace>";
    }

    void ShowLeaderboardReturnPanel()
    {
        if (_leaderboardReturnPanel != null) ShowOnly(_leaderboardReturnPanel);
        else ShowStartMenu();
    }

    // ---------- Building ----------

    public void BuildUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) return;
        RectTransform root = canvas.transform as RectTransform;
        if (root == null) return;

        if (startMenuPanel == null)
        {
            startMenuPanel = CreatePanel(root, "StartMenu", new Color(0.03f, 0.06f, 0.1f, 0.92f));
            CreateLabel(startMenuPanel.transform, "Title", "AR SURVIVAL SHOOTER", new Vector2(0, 470), new Vector2(900, 100), 48);
            startButton = CreateButton(startMenuPanel.transform, "StartEasyButton", "START - EASY", new Vector2(0, 240), new Vector2(600, 100));
            hardButton = CreateButton(startMenuPanel.transform, "StartHardButton", "START - HARD", new Vector2(0, 100), new Vector2(600, 100));
            leaderboardButton = CreateButton(startMenuPanel.transform, "LeaderboardButton", "LEADERBOARD", new Vector2(0, -40), new Vector2(600, 100));
        }
        var instructions = startMenuPanel.transform.Find("Instructions");
        var instructionsText = instructions != null
            ? instructions.GetComponent<TMP_Text>()
            : CreateLabel(startMenuPanel.transform, "Instructions", "", new Vector2(0, -300), new Vector2(940, 340), 28);
        if (instructionsText != null)
            instructionsText.text = "Surface found! Press START to place the old barn.\nYour phone is your eyes - look around the real room.\n"
                + "Walk or use the MOVE pad, aim with the crosshair,\ntap the screen or hold SHOOT to fire.\nSurvive 60s or defeat 12 enemies to win.";

        if (hudPanel == null)
        {
            hudPanel = CreatePanel(root, "HUD", new Color(0, 0, 0, 0));
            hudPanel.GetComponent<Image>().raycastTarget = false;
            healthText = CreateLabel(hudPanel.transform, "HealthText", "HEALTH 100", Vector2.zero, new Vector2(360, 60), 26);
            healthBar = CreateSlider(hudPanel.transform, "Health", Vector2.zero, new Vector2(360, 26));
            scoreText = CreateLabel(hudPanel.transform, "Score", "SCORE 0", Vector2.zero, new Vector2(300, 60), 26);
            timerText = CreateLabel(hudPanel.transform, "Timer", "TIME 60", Vector2.zero, new Vector2(220, 60), 34);
            shootButton = CreateButton(hudPanel.transform, "ShootButton", "SHOOT", Vector2.zero, new Vector2(240, 240));
            var overlayObject = new GameObject("DamageOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayObject.transform.SetParent(hudPanel.transform, false);
            overlayObject.transform.SetAsFirstSibling();
            var overlayRect = (RectTransform)overlayObject.transform;
            overlayRect.anchorMin = Vector2.zero; overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero; overlayRect.offsetMax = Vector2.zero;
            var overlay = overlayObject.GetComponent<Image>(); overlay.color = new Color(1, 0, 0, 0); overlay.raycastTarget = false;
            var player = FindObjectOfType<PlayerController>();
            if (player != null) player.damageOverlay = overlay;
        }

        if (endPanel == null)
        {
            endPanel = CreatePanel(root, "EndPanel", new Color(0.03f, 0.06f, 0.1f, 0.94f));
            CreateLabel(endPanel.transform, "GameOverTitle", "GAME OVER", Vector2.zero, new Vector2(800, 100), 48);
            finalScoreText = CreateLabel(endPanel.transform, "FinalScore", "Score: 0", Vector2.zero, new Vector2(650, 64), 34);
            enemiesDefeatedText = CreateLabel(endPanel.transform, "Defeated", "Enemies: 0", Vector2.zero, new Vector2(650, 64), 30);
            timeSurvivedText = CreateLabel(endPanel.transform, "Survived", "Time: 0s", Vector2.zero, new Vector2(650, 64), 30);
            restartButton = CreateButton(endPanel.transform, "RestartButton", "RESTART", Vector2.zero, new Vector2(560, 90));
            mainMenuButton = CreateButton(endPanel.transform, "MainMenuButton", "MAIN MENU", Vector2.zero, new Vector2(560, 90));
        }

        if (leaderboardPanel == null)
        {
            leaderboardPanel = CreatePanel(root, "LeaderboardPanel", new Color(0.03f, 0.06f, 0.1f, 0.94f));
            CreateLabel(leaderboardPanel.transform, "LeaderboardTitle", "LEADERBOARD", new Vector2(0, 400), new Vector2(800, 90), 42);
            leaderboardText = CreateLabel(leaderboardPanel.transform, "Entries", "No games played yet.", new Vector2(0, 0), new Vector2(800, 500), 30);
            leaderboardBackButton = CreateButton(leaderboardPanel.transform, "BackButton", "BACK", new Vector2(0, -520), new Vector2(500, 90));
        }
        var lbTitle = leaderboardPanel.transform.Find("LeaderboardTitle");
        if (lbTitle != null && lbTitle.TryGetComponent(out TMP_Text lbTitleText)) lbTitleText.text = "LEADERBOARD - TOP 10";
        if (leaderboardText != null)
        {
            Place(leaderboardText.transform, new Vector2(0, 0), new Vector2(980, 700));
            leaderboardText.fontSize = 32;
            leaderboardText.richText = true;
        }

        if (placementPrompt == null)
            placementPrompt = CreateLabel(root, "PlacementPrompt", "Point at a detected surface to place the barn", new Vector2(0, -730), new Vector2(900, 110), 30);
        var placement = FindObjectOfType<ARPlacementController>();
        if (placement != null) placement.placementPrompt = placementPrompt;

        if (scanningPrompt == null)
        {
            var scan = CreateLabel(root, "ScanningPrompt", "Scanning... move your phone slowly over the floor or a table", new Vector2(0, -730), new Vector2(950, 130), 30);
            scanningPrompt = scan.gameObject;
        }

        if (hudPanel != null)
        {
            if (livesText == null) livesText = CreateLabel(hudPanel.transform, "LivesText", "LIVES 3/3", Vector2.zero, new Vector2(360, 60), 30);
            if (killsText == null) killsText = CreateLabel(hudPanel.transform, "KillsText", "KILLS 0", Vector2.zero, new Vector2(300, 60), 26);
            if (messageText == null) messageText = CreateLabel(hudPanel.transform, "MessageText", "", Vector2.zero, new Vector2(900, 120), 56);
            if (controlsHint == null) controlsHint = CreateLabel(hudPanel.transform, "ControlsHint", "Move your phone to look around - tap or hold SHOOT to fire", Vector2.zero, new Vector2(900, 60), 26);
            if (shootButton == null) shootButton = CreateButton(hudPanel.transform, "ShootButton", "SHOOT", Vector2.zero, new Vector2(240, 240));
            if (turnLeftButton == null) turnLeftButton = CreateButton(hudPanel.transform, "TurnLeftButton", "TURN LEFT", Vector2.zero, new Vector2(190, 95));
            if (turnRightButton == null) turnRightButton = CreateButton(hudPanel.transform, "TurnRightButton", "TURN RIGHT", Vector2.zero, new Vector2(190, 95));
            if (hudPanel.transform.Find("MovementPad") == null) CreateMovementPad(hudPanel.transform);
            if (hudPanel.transform.Find("TopBar") == null)
            {
                var bar = CreatePanel(hudPanel.transform, "TopBar", new Color(0f, 0f, 0f, 0.45f));
                bar.GetComponent<Image>().raycastTarget = false;
                var barRect = (RectTransform)bar.transform;
                barRect.anchorMin = new Vector2(0, 1); barRect.anchorMax = new Vector2(1, 1);
                barRect.pivot = new Vector2(0.5f, 1); barRect.sizeDelta = new Vector2(0, 230); barRect.anchoredPosition = Vector2.zero;
                bar.transform.SetSiblingIndex(1);
            }
            // The full-screen HUD panel must not swallow touches, or dragging the camera would never reach the game.
            if (hudPanel.TryGetComponent(out Image hudImage)) hudImage.raycastTarget = false;
            foreach (var label in hudPanel.GetComponentsInChildren<TMP_Text>(true)) label.raycastTarget = false;
            if (healthBar != null) healthBar.interactable = false;
            LayoutHUD();
        }
        if (endPanel != null)
        {
            if (resultTitle == null)
            {
                var existingTitle = endPanel.transform.Find("GameOverTitle");
                resultTitle = existingTitle != null ? existingTitle.GetComponent<TMP_Text>() : CreateLabel(endPanel.transform, "GameOverTitle", "YOU DIED", Vector2.zero, new Vector2(800, 100), 48);
            }
            if (bestScoreText == null) bestScoreText = CreateLabel(endPanel.transform, "BestScore", "High score: 0", Vector2.zero, new Vector2(650, 60), 30);
            if (endLeaderboardButton == null) endLeaderboardButton = CreateButton(endPanel.transform, "EndLeaderboardButton", "LEADERBOARD", Vector2.zero, new Vector2(560, 90));
            LayoutEndPanel();
        }

        if (!Application.isPlaying)
        {
            if (startMenuPanel) startMenuPanel.SetActive(true);
            if (hudPanel) hudPanel.SetActive(false);
            if (endPanel) endPanel.SetActive(false);
            if (leaderboardPanel) leaderboardPanel.SetActive(false);
            if (placementPrompt) placementPrompt.gameObject.SetActive(false);
            if (scanningPrompt) scanningPrompt.SetActive(false);
        }

        // A tiny white sprite is tinted by each Image and avoids relying on editor-only built-in assets.
        foreach (Image image in GetComponentsInChildren<Image>(true))
            if (image.sprite == null) image.sprite = SolidSprite;
    }

    /// <summary>Positions every HUD element (reference 1080x1920, centre anchors) so old scenes get the same layout.</summary>
    void LayoutHUD()
    {
        // Top bar: lives | time | score, with health and kills underneath.
        Place(livesText, new Vector2(-330, 880), new Vector2(360, 60));
        Place(timerText, new Vector2(0, 880), new Vector2(260, 70));
        Place(scoreText, new Vector2(330, 880), new Vector2(320, 60));
        Place(healthText, new Vector2(-330, 815), new Vector2(360, 50));
        Place(healthBar, new Vector2(-330, 772), new Vector2(360, 28));
        Place(killsText, new Vector2(330, 815), new Vector2(320, 50));
        if (timerText) timerText.fontSize = 40;
        if (livesText) livesText.color = new Color(1f, 0.55f, 0.55f);
        Place(messageText, new Vector2(0, 450), new Vector2(950, 130));
        if (messageText) { messageText.color = new Color(1f, 0.85f, 0.3f); messageText.raycastTarget = false; messageText.gameObject.SetActive(false); }
        Place(controlsHint, new Vector2(0, -520), new Vector2(950, 60));
        if (controlsHint) controlsHint.text = "Move your phone to look around - tap or hold SHOOT to fire";
        SetButtonLabel(turnLeftButton, "TURN LEFT");
        SetButtonLabel(turnRightButton, "TURN RIGHT");
        // Tilt buttons from the old orbit camera: looking up/down is now done by tilting the phone.
        foreach (var obsolete in new[] { "TiltUpButton", "TiltDownButton" })
        {
            var t = hudPanel.transform.Find(obsolete);
            if (t != null) t.gameObject.SetActive(false);
        }

        // Crosshair in the middle of the screen: shots go where the phone points.
        var crosshair = hudPanel.transform.Find("Crosshair");
        var crosshairText = crosshair != null ? crosshair.GetComponent<TMP_Text>() : CreateLabel(hudPanel.transform, "Crosshair", "+", Vector2.zero, new Vector2(120, 120), 90);
        Place(crosshairText, Vector2.zero, new Vector2(120, 120));
        if (crosshairText) { crosshairText.raycastTarget = false; crosshairText.color = new Color(1f, 1f, 1f, 0.85f); }
        if (controlsHint) controlsHint.raycastTarget = false;

        // Bottom: movement pad (left), camera buttons (centre), shoot (right).
        var pad = hudPanel.transform.Find("MovementPad");
        if (pad != null) Place(pad, new Vector2(-370, -720), new Vector2(260, 260));
        Place(turnLeftButton, new Vector2(-100, -720), new Vector2(190, 130));
        Place(turnRightButton, new Vector2(100, -720), new Vector2(190, 130));
        Place(shootButton, new Vector2(375, -720), new Vector2(250, 250));
        if (shootButton && shootButton.TryGetComponent(out Image shootImage)) shootImage.color = new Color(0.85f, 0.2f, 0.2f, 0.95f);
        var shootLabel = shootButton != null ? shootButton.GetComponentInChildren<TMP_Text>() : null;
        if (shootLabel) shootLabel.fontSize = 36;
    }

    void LayoutEndPanel()
    {
        Place(resultTitle, new Vector2(0, 420), new Vector2(900, 120));
        if (resultTitle) resultTitle.fontSize = 72;
        Place(finalScoreText, new Vector2(0, 270), new Vector2(700, 70));
        Place(enemiesDefeatedText, new Vector2(0, 190), new Vector2(700, 60));
        Place(timeSurvivedText, new Vector2(0, 120), new Vector2(700, 60));
        Place(bestScoreText, new Vector2(0, 40), new Vector2(700, 60));
        if (bestScoreText) bestScoreText.color = new Color(1f, 0.85f, 0.3f);
        Place(restartButton, new Vector2(0, -120), new Vector2(560, 100));
        Place(mainMenuButton, new Vector2(0, -250), new Vector2(560, 100));
        Place(endLeaderboardButton, new Vector2(0, -380), new Vector2(560, 100));
    }

    static void SetButtonLabel(Button button, string label)
    {
        var text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        if (text != null) text.text = label;
    }

    static void Place(Component c, Vector2 position, Vector2 size)
    {
        if (c == null || !(c.transform is RectTransform rect)) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static Sprite SolidSprite
    {
        get
        {
            if (_solidSprite != null) return _solidSprite;
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "Runtime UI White Pixel";
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);
            _solidSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            _solidSprite.name = "Runtime UI Solid Sprite";
            _solidSprite.hideFlags = HideFlags.HideAndDontSave;
            return _solidSprite;
        }
    }

    void CreateMovementPad(Transform parent)
    {
        var padObject = new GameObject("MovementPad", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MobileMovementPad));
        padObject.transform.SetParent(parent, false);
        var rect = (RectTransform)padObject.transform;
        rect.sizeDelta = new Vector2(260, 260);
        rect.anchoredPosition = new Vector2(-370, -720);
        var image = padObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.18f, 0.25f, 0.7f);
        CreateLabel(padObject.transform, "MoveLabel", "MOVE", new Vector2(0, 150), new Vector2(200, 40), 24).raycastTarget = false;
        var knobObject = new GameObject("Knob", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        knobObject.transform.SetParent(padObject.transform, false);
        var knobRect = (RectTransform)knobObject.transform;
        knobRect.sizeDelta = new Vector2(110, 110);
        var knobImage = knobObject.GetComponent<Image>();
        knobImage.color = new Color(0.25f, 0.72f, 0.95f, 0.9f);
        knobImage.raycastTarget = false;
        var pad = padObject.GetComponent<MobileMovementPad>();
        pad.knob = knobRect;
        pad.radius = 100f;
    }

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.color = color;
        return go;
    }

    static TMP_Text CreateLabel(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.sizeDelta = size; rect.anchoredPosition = position;
        var text = go.GetComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center; text.color = Color.white; text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.sizeDelta = size; rect.anchoredPosition = position;
        var image = go.GetComponent<Image>(); image.color = new Color(0.12f, 0.55f, 0.82f, 0.96f);
        var button = go.GetComponent<Button>(); button.targetGraphic = image;
        var labelText = CreateLabel(go.transform, "Label", label, Vector2.zero, size, 22);
        var labelRect = (RectTransform)labelText.transform; labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
        return button;
    }

    static Slider CreateSlider(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Slider)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.sizeDelta = size; rect.anchoredPosition = position;
        var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); bg.transform.SetParent(go.transform, false);
        var bgRect = (RectTransform)bg.transform; bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one; bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        var backgroundImage = bg.GetComponent<Image>(); backgroundImage.color = new Color(0.25f, 0.25f, 0.25f, 0.9f);
        var fillArea = new GameObject("Fill Area", typeof(RectTransform)); fillArea.transform.SetParent(go.transform, false);
        var fillAreaRect = (RectTransform)fillArea.transform; fillAreaRect.anchorMin = Vector2.zero; fillAreaRect.anchorMax = Vector2.one; fillAreaRect.offsetMin = new Vector2(4, 4); fillAreaRect.offsetMax = new Vector2(-4, -4);
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); fill.transform.SetParent(fillArea.transform, false);
        var fillRect = (RectTransform)fill.transform; fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one; fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
        var fillImage = fill.GetComponent<Image>(); fillImage.color = new Color(0.15f, 0.85f, 0.32f, 1);
        var slider = go.GetComponent<Slider>(); slider.targetGraphic = bg.GetComponent<Image>(); slider.fillRect = fillRect; slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
        slider.interactable = false;
        return slider;
    }
}
