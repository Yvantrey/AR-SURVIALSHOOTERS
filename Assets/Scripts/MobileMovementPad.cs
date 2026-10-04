using UnityEngine;
using UnityEngine.EventSystems;

public class MobileMovementPad : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public RectTransform knob;
    public float radius = 75f;
    PlayerController _player;
    RectTransform _rect;

    void Awake()
    {
        _rect = transform as RectTransform;
        _player = PlayerController.Instance != null ? PlayerController.Instance : FindObjectOfType<PlayerController>();
    }

    public void OnPointerDown(PointerEventData eventData) => UpdateInput(eventData);
    public void OnDrag(PointerEventData eventData) => UpdateInput(eventData);

    public void OnPointerUp(PointerEventData eventData)
    {
        if (knob != null) knob.anchoredPosition = Vector2.zero;
        if (_player != null) _player.SetMoveInput(Vector2.zero);
    }

    void UpdateInput(PointerEventData eventData)
    {
        if (_rect == null) return;
        if (_player == null) _player = PlayerController.Instance != null ? PlayerController.Instance : FindObjectOfType<PlayerController>();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
        Vector2 value = Vector2.ClampMagnitude(local / Mathf.Max(1f, radius), 1f);
        if (knob != null) knob.anchoredPosition = value * radius;
        if (_player != null) _player.SetMoveInput(value);
    }
}
