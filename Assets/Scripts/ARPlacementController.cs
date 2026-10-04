using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Flow: scan -> planes detected (start menu appears) -> Start -> barn anchored to a plane -> play.
/// The barn is parented to an ARAnchor so it stays attached to the real surface.
/// </summary>
public class ARPlacementController : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public GameObject gameWorldRoot;
    public TMP_Text placementPrompt;
    [Tooltip("Uniform scale applied to the barn world when it is placed. 1 = authored size.")]
    public float worldScale = 1f;
    [Tooltip("Place automatically on the plane at the centre of the screen when Start is pressed.")]
    public bool autoPlaceAtScreenCenter = true;

    bool _placed;
    bool _surfaceReady;
    bool _hasPose;
    Pose _lastPose;
    ARAnchor _anchor;
    readonly List<ARRaycastHit> _hits = new();

    public bool SurfaceReady => _surfaceReady;
    public bool HasPlacedWorld => _hasPose;

    void Start()
    {
        if (planeManager == null) planeManager = FindObjectOfType<ARPlaneManager>();
        if (raycastManager == null) raycastManager = FindObjectOfType<ARRaycastManager>();
        // Anchors need a manager on the XR Origin to be tracked against the real surface.
        if (planeManager != null && FindObjectOfType<ARAnchorManager>() == null) planeManager.gameObject.AddComponent<ARAnchorManager>();
        // The XRI starter "Object Spawner" drops sample cubes on every tap; it fights with placement and shooting.
        var sampleSpawner = GameObject.Find("Object Spawner");
        if (sampleSpawner != null) sampleSpawner.SetActive(false);
        if (gameWorldRoot != null) gameWorldRoot.SetActive(false);
        UIManager.Instance?.ShowScanningPrompt();
    }

    void Update()
    {
        var state = GameManager.Instance != null ? GameManager.Instance.State : GameState.Start;
        if (state == GameState.Start && !_surfaceReady)
        {
            if (AnyTrackedPlane() || NoARDevice())
            {
                _surfaceReady = true;
                UIManager.Instance?.ShowStartMenu();
            }
            return;
        }
        if (_placed || state != GameState.Placing) return;

        if (NoARDevice()) { PlaceOnVirtualFloor(); return; }

        if (autoPlaceAtScreenCenter && TryPlace(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f))) return;

        TouchControl touch = Touchscreen.current != null ? Touchscreen.current.primaryTouch : null;
        if (touch != null && touch.press.wasPressedThisFrame)
        {
            int pointerId = touch.touchId.ReadValue();
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(pointerId))
                TryPlace(touch.position.ReadValue());
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
                 (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            if (!TryPlace(Mouse.current.position.ReadValue()) && raycastManager == null)
                PlaceAt(new Pose(Vector3.zero, Quaternion.identity)); // editor without AR: place at origin
        }
    }

    /// <summary>Editor / desktop with no AR session: there are no real planes to detect.</summary>
    bool NoARDevice() =>
        !Application.isMobilePlatform && ARSession.state < ARSessionState.Ready && Time.timeSinceLevelLoad > 2.5f;

    /// <summary>Without AR, assume the camera is held ~1.5 m above the floor and put the barn just ahead.</summary>
    void PlaceOnVirtualFloor()
    {
        _anchor = null;
        Transform cam = Camera.main != null ? Camera.main.transform : transform;
        Vector3 forward = cam.forward; forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();
        Vector3 floor = cam.position + forward * 3f + Vector3.down * 1.5f;
        PlaceAt(new Pose(floor, Quaternion.LookRotation(forward)));
    }

    bool AnyTrackedPlane()
    {
        if (planeManager == null) planeManager = FindObjectOfType<ARPlaneManager>();
        if (planeManager == null) return false;
        foreach (ARPlane plane in planeManager.trackables)
            if (plane != null && plane.trackingState == TrackingState.Tracking) return true;
        return false;
    }

    bool TryPlace(Vector2 screenPos)
    {
        if (raycastManager == null || gameWorldRoot == null) return false;
        if (!raycastManager.Raycast(screenPos, _hits, TrackableType.PlaneWithinPolygon)) return false;
        var hit = _hits[0];
        // Face the barn towards the player (yaw only) so the camera starts behind the avatar.
        Vector3 toCamera = Camera.main != null ? Camera.main.transform.position - hit.pose.position : Vector3.back;
        toCamera.y = 0f;
        Quaternion rotation = toCamera.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(-toCamera) : hit.pose.rotation;
        AttachAnchor(hit);
        PlaceAt(new Pose(hit.pose.position, rotation));
        return true;
    }

    void AttachAnchor(ARRaycastHit hit)
    {
        if (_anchor != null) { Destroy(_anchor.gameObject); _anchor = null; }
        var anchorGo = new GameObject("BarnAnchor");
        anchorGo.transform.SetPositionAndRotation(hit.pose.position, hit.pose.rotation);
        _anchor = anchorGo.AddComponent<ARAnchor>();
    }

    void PlaceAt(Pose pose)
    {
        if (gameWorldRoot == null) return;
        _lastPose = pose;
        _hasPose = true;
        var root = gameWorldRoot.transform;
        root.SetParent(_anchor != null ? _anchor.transform : null, true);
        root.SetPositionAndRotation(pose.position, pose.rotation);
        root.localScale = Vector3.one * Mathf.Max(0.01f, worldScale);
        gameWorldRoot.SetActive(true);
        _placed = true;
        SetPlanesVisible(false);
        if (placementPrompt != null) placementPrompt.gameObject.SetActive(false);
        GameManager.Instance?.OnWorldPlaced();
    }

    /// <summary>Restart on the same spot without re-scanning.</summary>
    public void ReuseCurrentPlacement()
    {
        if (!_hasPose) { BeginPlacement(); return; }
        _placed = false;
        Pose pose = _anchor != null
            ? new Pose(gameWorldRoot.transform.position, gameWorldRoot.transform.rotation)
            : _lastPose;
        PlaceAt(pose);
    }

    public void BeginPlacement()
    {
        _placed = false;
        _surfaceReady = true;
        SetPlanesVisible(true);
        if (gameWorldRoot != null) gameWorldRoot.SetActive(false);
        if (placementPrompt != null) placementPrompt.gameObject.SetActive(true);
    }

    public void ResetPlacement()
    {
        _placed = false;
        _hasPose = false;
        _surfaceReady = AnyTrackedPlane();
        SetPlanesVisible(true);
        if (gameWorldRoot != null)
        {
            gameWorldRoot.transform.SetParent(null, true);
            gameWorldRoot.SetActive(false);
        }
        if (_anchor != null) { Destroy(_anchor.gameObject); _anchor = null; }
        if (placementPrompt != null) placementPrompt.gameObject.SetActive(false);
    }

    void SetPlanesVisible(bool visible)
    {
        if (planeManager == null) return;
        foreach (ARPlane plane in planeManager.trackables)
            if (plane != null) plane.gameObject.SetActive(visible);
    }
}
