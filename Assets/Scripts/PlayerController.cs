using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// First-person AR player. Lives on the AR camera: the phone's real camera (with device tracking
/// left on) is the player's view, so the real surroundings stay visible while playing.
/// The joystick / turn buttons add virtual movement on top of walking around for real.
/// Enemies target a floor marker that follows the camera, and shots fire from the camera.
/// </summary>
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Stats")]
    public int maxHealth = 100;
    public int maxLives = 3;
    public float invulnerableAfterLifeLost = 1.5f;

    [Header("Shooting")]
    public Transform firePoint;
    public float fireRate = 0.25f;
    public float bulletSpeed = 14f;
    public int bulletDamage = 1;
    public float aimRange = 25f;
    [Tooltip("Enemies within this angle of the screen centre are auto-aimed.")]
    public float autoAimAngle = 25f;
    public bool tapToShoot = true;

    [Header("Movement")]
    public float movementSpeed = 2f;
    [Tooltip("How far beyond the barn walls the player may move with the joystick (arena units).")]
    public float arenaMargin = 3f;
    [Tooltip("Height of the player's chest below the phone, used as the enemies' aim point.")]
    public float chestOffset = 0.25f;
    public float hitboxRadius = 0.35f;

    [Header("First Person View")]
    [Tooltip("The XR rig's AR camera (the phone's real camera). Found automatically if empty.")]
    public Camera playerCamera;
    [Tooltip("First-person arms (arms@sniper). Kept attached to the AR camera. Found automatically if empty.")]
    public Transform firstPersonArms;
    public Vector3 armsLocalPosition = new Vector3(0.3f, -0.3f, 0.5f);
    public float editorLookSensitivity = 0.15f;

    [Header("Feedback")]
    public Image damageOverlay; // red flash UI image

    // Kept so older scenes that serialized these fields still load cleanly.
    [HideInInspector] public GameObject avatarPrefab;
    [HideInInspector] public float arenaCameraHeight = 4f;

    int _currentHealth;
    int _lives;
    float _fireTimer;
    float _invulnerableTimer;
    bool _isDead;
    bool _firing;
    Vector2 _moveInput;
    Coroutine _flashRoutine;

    Transform _rig;
    Transform _arenaRoot;
    Bounds _arenaLocalBounds;
    Transform _floorMarker;
    Transform _hitbox;
    bool _arenaActive;
    Vector3 _savedRigPosition;
    Quaternion _savedRigRotation;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _rig = transform.root;
        _currentHealth = maxHealth;
        _lives = maxLives;
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;
        AttachArmsToCamera();
        CreateHitbox();
        // Real camera image behind the game: AR camera feed on the phone, webcam in the Editor.
        if (!Application.isMobilePlatform && playerCamera != null && playerCamera.GetComponent<WebcamBackground>() == null)
            playerCamera.gameObject.AddComponent<WebcamBackground>().targetCamera = playerCamera;
    }

    void Start() => UIManager.Instance?.UpdateHUD();

    /// <summary>The player's eyes: the AR camera, which device tracking moves with the phone.</summary>
    Transform View => playerCamera != null ? playerCamera.transform : transform;

    void AttachArmsToCamera()
    {
        if (firstPersonArms == null)
        {
            var found = GameObject.Find("arms@sniper");
            if (found != null) firstPersonArms = found.transform;
        }
        if (firstPersonArms == null || playerCamera == null) return;
        if (firstPersonArms.parent != View)
        {
            firstPersonArms.SetParent(View, false);
            firstPersonArms.localPosition = armsLocalPosition;
            firstPersonArms.localRotation = Quaternion.identity;
        }
        // The arms are just a view model: they must never block bullets or count as the hitbox.
        foreach (var col in firstPersonArms.GetComponentsInChildren<Collider>(true)) col.enabled = false;
        if (firstPersonArms.CompareTag("Player")) firstPersonArms.tag = "Untagged";
        firstPersonArms.gameObject.SetActive(true);
        // Shoot from just in front of the gun.
        if (firePoint != null && firePoint.parent != View) firePoint.SetParent(View, false);
    }

    void Update()
    {
        EditorMouseLook();
        if (GameManager.Instance?.State != GameState.Playing) return;
        _fireTimer -= Time.deltaTime;
        _invulnerableTimer -= Time.deltaTime;
        MovePlayer();
        UpdateFloorMarker();
        if (_firing || (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)) TryShoot();
        else if (tapToShoot && TappedEmptyScreen()) TryShoot();
    }

    /// <summary>
    /// Without an AR device (Editor / desktop) nothing tracks the camera, so hold the right mouse
    /// button and move the mouse to look around. On the phone, the device's motion does this.
    /// </summary>
    void EditorMouseLook()
    {
        if (Application.isMobilePlatform || Mouse.current == null || !Mouse.current.rightButton.isPressed) return;
        Vector2 delta = Mouse.current.delta.ReadValue() * editorLookSensitivity;
        Vector3 euler = View.localEulerAngles;
        float pitch = euler.x > 180f ? euler.x - 360f : euler.x;
        pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
        View.localRotation = Quaternion.Euler(pitch, euler.y + delta.x, 0f);
    }

    // ---------- Public API used by UI / game flow ----------

    public void Shoot() => TryShoot();
    public void SetFiring(bool firing) => _firing = firing;
    public void SetMoveInput(Vector2 value) => _moveInput = Vector2.ClampMagnitude(value, 1f);

    /// <summary>Turns the player (rotates the XR rig around the phone) for players who can't spin around for real.</summary>
    public void RotateArenaCamera(float degrees)
    {
        if (!_arenaActive || _rig == null) return;
        _rig.RotateAround(View.position, Vector3.up, degrees);
    }

    // Tilting is done by physically tilting the phone; kept so UI bindings stay valid.
    public void TiltArenaCamera(float degrees) { }
    public void ZoomArenaCamera(float delta) { }

    /// <summary>The player's position on the barn floor (what melee enemies walk to).</summary>
    public Transform Body => _floorMarker != null ? _floorMarker : View;
    /// <summary>What enemy shooters aim at: just below the phone.</summary>
    public Vector3 AimPoint => View.position - Vector3.up * (chestOffset * ArenaScale);
    public Bounds ArenaLocalBounds => _arenaLocalBounds;
    public Transform ArenaRoot => _arenaRoot;

    public void ConfigureArenaCamera(Transform arenaRoot)
    {
        if (arenaRoot == null) return;
        if (_rig == null) _rig = transform.root;
        if (!_arenaActive && _rig != null)
        {
            _savedRigPosition = _rig.position;
            _savedRigRotation = _rig.rotation;
        }
        _arenaRoot = arenaRoot;
        _arenaLocalBounds = ComputeLocalBounds(arenaRoot);
        _moveInput = Vector2.zero;
        _firing = false;
        if (_floorMarker == null) _floorMarker = new GameObject("PlayerFloorMarker").transform;
        _floorMarker.SetParent(arenaRoot, false);
        _arenaActive = true;
        UpdateFloorMarker();
        if (_hitbox != null) _hitbox.localScale = Vector3.one * ArenaScale;
    }

    public void ResetArenaCamera()
    {
        _moveInput = Vector2.zero;
        _firing = false;
        // Undo any joystick movement so the real and virtual worlds line up again.
        if (_arenaActive && _rig != null) _rig.SetPositionAndRotation(_savedRigPosition, _savedRigRotation);
        if (_floorMarker != null) _floorMarker.SetParent(null, false);
        _arenaRoot = null;
        _arenaActive = false;
    }

    public void ResetForNewGame()
    {
        _currentHealth = maxHealth;
        _lives = maxLives;
        _isDead = false;
        _fireTimer = 0f;
        _invulnerableTimer = 0f;
        ResetArenaCamera();
        if (damageOverlay != null) damageOverlay.color = new Color(1, 0, 0, 0f);
        UIManager.Instance?.UpdateHUD();
    }

    // ---------- Movement ----------

    float ArenaScale => _arenaRoot != null ? Mathf.Max(0.01f, _arenaRoot.lossyScale.y) : 1f;

    void CreateHitbox()
    {
        // The camera must keep its MainCamera tag, so the "Player" hitbox is a child object.
        var go = new GameObject("PlayerHitbox") { tag = "Player" };
        go.transform.SetParent(View, false);
        go.transform.localPosition = Vector3.down * chestOffset * 0.5f;
        var sphere = go.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = hitboxRadius;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        _hitbox = go.transform;
    }

    void MovePlayer()
    {
        if (!_arenaActive || _rig == null) return;
        Vector2 input = _moveInput;
        if (Keyboard.current != null)
        {
            Vector2 keys = new Vector2(
                (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0),
                (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0));
            if (keys != Vector2.zero) input = keys.normalized;
            if (Keyboard.current.qKey.isPressed) RotateArenaCamera(-90f * Time.deltaTime);
            if (Keyboard.current.eKey.isPressed) RotateArenaCamera(90f * Time.deltaTime);
        }
        if (input.sqrMagnitude < 0.001f) return;

        // Walk along the floor in the direction the phone is facing.
        Vector3 forward = View.forward; forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = View.up; // phone pointing straight down
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 step = (right * input.x + forward * input.y) * (movementSpeed * ArenaScale * Time.deltaTime);

        Vector3 next = _arenaRoot.InverseTransformPoint(View.position + step);
        if (_arenaLocalBounds.size.x > 0.01f)
        {
            float m = arenaMargin;
            if (next.x < _arenaLocalBounds.min.x - m || next.x > _arenaLocalBounds.max.x + m ||
                next.z < _arenaLocalBounds.min.z - m || next.z > _arenaLocalBounds.max.z + m) return;
        }
        _rig.position += step;
    }

    void UpdateFloorMarker()
    {
        if (_floorMarker == null || _arenaRoot == null) return;
        Vector3 local = _arenaRoot.InverseTransformPoint(View.position);
        local.y = 0f;
        _floorMarker.localPosition = local;
        Vector3 forward = View.forward; forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f) _floorMarker.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    static Bounds ComputeLocalBounds(Transform root)
    {
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            // Only the barn counts: skip enemies and bullets.
            if (r.GetComponentInParent<EnemyBase>() != null || r.GetComponentInParent<Rigidbody>() != null) continue;
            Bounds wb = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = wb.center + Vector3.Scale(wb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 local = root.InverseTransformPoint(corner);
                if (first) { bounds = new Bounds(local, Vector3.zero); first = false; }
                else bounds.Encapsulate(local);
            }
        }
        return bounds;
    }

    // ---------- Combat ----------

    bool TappedEmptyScreen()
    {
        TouchControl touch = Touchscreen.current != null ? Touchscreen.current.primaryTouch : null;
        if (touch != null && touch.press.wasPressedThisFrame) return !PointerOverUI(touch.touchId.ReadValue());
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !PointerOverUI();
    }

    bool PointerOverUI(int fingerId = -1)
    {
        var events = EventSystem.current;
        if (events == null) return false;
        return fingerId >= 0 ? events.IsPointerOverGameObject(fingerId) : events.IsPointerOverGameObject();
    }

    void TryShoot()
    {
        if (_isDead || _fireTimer > 0f || GameManager.Instance?.State != GameState.Playing) return;
        _fireTimer = fireRate;
        var proj = ObjectPool.Instance?.GetProjectile();
        if (proj == null) return;
        Transform origin = firePoint != null ? firePoint : View;
        // Shoot where the phone points; snap to an enemy that is close to the crosshair.
        Vector3 aimDirection = View.forward;
        EnemyBase target = FindAimTarget();
        if (target != null) aimDirection = (target.AimPoint - origin.position).normalized;
        proj.transform.SetPositionAndRotation(origin.position, Quaternion.LookRotation(aimDirection));
        proj.Init(isEnemyProjectile: false, bulletDamage, bulletSpeed * ArenaScale);
        AudioManager.Instance?.PlayPlayerShoot(origin.position);
    }

    EnemyBase FindAimTarget()
    {
        var gm = GameManager.Instance;
        if (gm == null) return null;
        EnemyBase best = null;
        float bestAngle = autoAimAngle;
        float range = aimRange * ArenaScale;
        foreach (var enemy in gm.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            Vector3 to = enemy.AimPoint - View.position;
            if (to.magnitude > range) continue;
            float angle = Vector3.Angle(View.forward, to);
            if (angle >= bestAngle) continue;
            bestAngle = angle;
            best = enemy;
        }
        return best;
    }

    public void TakeDamage(int amount)
    {
        if (_isDead || _invulnerableTimer > 0f || GameManager.Instance?.State != GameState.Playing) return;
        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        bool lostLife = false;
        if (_currentHealth <= 0)
        {
            _lives--;
            lostLife = true;
            if (_lives > 0)
            {
                _currentHealth = maxHealth;
                _invulnerableTimer = invulnerableAfterLifeLost;
            }
        }
        UIManager.Instance?.UpdateHUD();
        if (lostLife && _lives > 0) UIManager.Instance?.FlashMessage($"LIFE LOST! {_lives} LEFT");
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(DamageFlash(lostLife ? 0.6f : 0.35f));
        if (_lives <= 0) Die();
    }

    IEnumerator DamageFlash(float alpha)
    {
        if (damageOverlay == null) yield break;
        damageOverlay.color = new Color(1, 0, 0, alpha);
        yield return new WaitForSeconds(0.2f);
        damageOverlay.color = new Color(1, 0, 0, 0f);
    }

    void Die()
    {
        _isDead = true;
        _lives = 0;
        _firing = false;
        AudioManager.Instance?.PlayPlayerDeath(View.position);
        GameManager.Instance?.TriggerGameOver();
    }

    public float HealthPercent => (float)_currentHealth / maxHealth;
    public int CurrentHealth => _currentHealth;
    public int CurrentLives => _lives;
}
