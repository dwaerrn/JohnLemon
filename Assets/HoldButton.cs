using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Put this on a UI Button (or any UI Image). While the button is held down, it adds
/// its value to a shared static input axis. The character reads that axis.
/// The input is static because the character is spawned at runtime, so scene buttons
/// can't hold a reference to it in the Inspector.
/// </summary>
public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum Axis { Horizontal, Vertical }

    [Tooltip("Horizontal = left/right, Vertical = forward/back")]
    public Axis axis = Axis.Horizontal;

    [Tooltip("-1 for Left / Back, +1 for Right / Forward")]
    public float value = 1f;

    // Shared input the character reads.
    public static float Horizontal { get; private set; }
    public static float Vertical { get; private set; }

    bool held;

    public void OnPointerDown(PointerEventData e) => Press();
    public void OnPointerUp(PointerEventData e) => Release();
    public void OnPointerExit(PointerEventData e) => Release(); // finger slid off the button
    void OnDisable() => Release();

    void Press()
    {
        if (held) return;
        held = true;
        Add(value);
    }

    void Release()
    {
        if (!held) return;
        held = false;
        Add(-value);
    }

    void Add(float v)
    {
        if (axis == Axis.Horizontal) Horizontal += v;
        else Vertical += v;
    }
}
