using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// On-screen joystick. Put this on a round UI Image (the pad) that has a smaller round
/// UI Image as a child (the knob). Drag the knob to move; it springs back on release.
/// The direction is shared through the static <see cref="Value"/>, because the character
/// is spawned at runtime, so scene UI can't hold a reference to it in the Inspector.
/// </summary>
public class CirclePad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Tooltip("The knob image (a child of this pad). It follows the finger.")]
    public RectTransform knob;

    [Tooltip("Pushes smaller than this (0..1 of the pad's radius) are ignored, so a resting thumb doesn't move him.")]
    [Range(0f, 0.9f)] public float deadZone = 0.15f;

    /// <summary>x = left(-1)/right(+1), y = back(-1)/forward(+1). Length is 0..1.</summary>
    public static Vector2 Value { get; private set; }

    RectTransform m_Pad;
    int m_PointerId = int.MinValue;

    void Awake() => m_Pad = (RectTransform)transform;

    public void OnPointerDown(PointerEventData e)
    {
        if (m_PointerId != int.MinValue) return; // already held by another finger
        m_PointerId = e.pointerId;
        UpdateKnob(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId == m_PointerId) UpdateKnob(e);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId == m_PointerId) Release();
    }

    void OnDisable() => Release();

    void UpdateKnob(PointerEventData e)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_Pad, e.position, e.pressEventCamera, out Vector2 local))
            return;

        // Measure from the pad's centre, whatever its pivot is.
        local -= m_Pad.rect.center;
        float radius = Mathf.Min(m_Pad.rect.width, m_Pad.rect.height) * 0.5f;
        Vector2 v = Vector2.ClampMagnitude(local / radius, 1f);

        if (knob != null) knob.anchoredPosition = v * radius;
        Value = v.magnitude < deadZone ? Vector2.zero : v;
    }

    void Release()
    {
        m_PointerId = int.MinValue;
        Value = Vector2.zero;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
    }
}
