using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementController : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public GameObject gameWorldRoot;

    bool _placed;
    readonly List<ARRaycastHit> _hits = new();

    void Update()
    {
        if (_placed || GameManager.Instance?.State != GameState.Start) return;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            TryPlace(Input.GetTouch(0).position);
        else if (Input.GetMouseButtonDown(0))
            TryPlace(Input.mousePosition);
    }

    void TryPlace(Vector2 screenPos)
    {
        if (!raycastManager.Raycast(screenPos, _hits, TrackableType.PlaneWithinPolygon)) return;
        gameWorldRoot.transform.SetPositionAndRotation(_hits[0].pose.position, _hits[0].pose.rotation);
        gameWorldRoot.SetActive(true);
        _placed = true;
    }
}
