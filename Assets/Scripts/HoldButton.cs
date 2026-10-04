using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>Button that reports press / release, e.g. hold SHOOT for continuous fire.</summary>
public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public UnityEvent onPress = new();
    public UnityEvent onRelease = new();
    bool _held;

    public void OnPointerDown(PointerEventData eventData)
    {
        _held = true;
        onPress.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData) => Release();
    public void OnPointerExit(PointerEventData eventData) => Release();
    void OnDisable() => Release();

    void Release()
    {
        if (!_held) return;
        _held = false;
        onRelease.Invoke();
    }
}
