using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// AR version of John Lemon's PlayerMovement.
/// Reads input from the on-screen HoldButtons (plus the keyboard in the Editor),
/// drives the "IsWalking" animator bool, and moves at Walk Speed (or with root motion if enabled).
/// Movement is camera-relative by default, so "Left" always means left on the screen,
/// whichever side of the marker you're looking from.
///
/// He moves by changing his transform relative to the marker (his parent), never by pinning a
/// world position/rotation. So when AR tracking refines or moves the marker's pose, he moves
/// with it instead of being left behind at the old spot.
/// </summary>
[RequireComponent(typeof(Animator))]
public class ARPlayerMovement : MonoBehaviour
{
    [Tooltip("How fast he turns to face the walking direction, in radians per second.")]
    public float turnSpeed = 20f;

    [Tooltip("On: left/right are relative to the phone camera. Off: relative to the marker.")]
    public bool cameraRelative = true;

    [Tooltip("Off: move at Walk Speed (needed because the Walk clip walks in place). " +
             "On: take the speed from the animation's root motion, like the tutorial.")]
    public bool useRootMotion = false;

    [Tooltip("Walking speed in the character's own units per second (he is about 1.35 units tall). " +
             "Scales with the character, so it looks the same at any size.")]
    public float walkSpeed = 1f;

    [Tooltip("Hide him until the marker is properly tracked, so he doesn't appear at the " +
             "rough first guess of the marker's position. Ignored when there's no tracked image (e.g. a scene copy).")]
    public bool waitForGoodTracking = true;

    Animator m_Animator;
    AudioSource m_AudioSource;
    Transform m_Camera;
    ARTrackedImage m_TrackedImage;
    Renderer[] m_Renderers;
    bool m_Shown = true;

    Vector3 m_Movement;

    void Start()
    {
        m_Animator = GetComponent<Animator>();
        m_AudioSource = GetComponent<AudioSource>();
        if (Camera.main != null) m_Camera = Camera.main.transform;

        // The Rigidbody isn't used for movement (it would pin him to a world position while
        // the marker moves). Keep it kinematic so physics never pushes him around.
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        m_TrackedImage = GetComponentInParent<ARTrackedImage>();
        m_Renderers = GetComponentsInChildren<Renderer>(true);
        if (waitForGoodTracking && m_TrackedImage != null) SetShown(false);
    }

    void Update()
    {
        if (!m_Shown)
        {
            if (m_TrackedImage.trackingState == TrackingState.Tracking) SetShown(true);
            else
            {
                m_Movement = Vector3.zero;
                m_Animator.SetBool("IsWalking", false);
                return;
            }
        }

        float horizontal = HoldButton.Horizontal;
        float vertical = HoldButton.Vertical;

#if UNITY_EDITOR && ENABLE_LEGACY_INPUT_MANAGER
        // Lets you test with WASD/arrow keys in the Editor.
        horizontal += Input.GetAxis("Horizontal");
        vertical += Input.GetAxis("Vertical");
#endif

        horizontal = Mathf.Clamp(horizontal, -1f, 1f);
        vertical = Mathf.Clamp(vertical, -1f, 1f);

        // "Up" is the marker's normal, so this also works on a tilted or wall-mounted marker.
        Vector3 up = transform.parent != null ? transform.parent.up : Vector3.up;

        Vector3 right, forward;
        if (cameraRelative && m_Camera != null)
        {
            right = Vector3.ProjectOnPlane(m_Camera.right, up).normalized;
            forward = Vector3.Cross(right, up); // stays valid even when looking straight down
        }
        else
        {
            Transform reference = transform.parent != null ? transform.parent : transform;
            right = reference.right;
            forward = reference.forward;
        }

        m_Movement = right * horizontal + forward * vertical;
        if (m_Movement.sqrMagnitude > 1f) m_Movement.Normalize();

        bool isWalking = m_Movement.sqrMagnitude > 0.0001f;
        m_Animator.SetBool("IsWalking", isWalking);

        if (m_AudioSource != null)
        {
            if (isWalking && !m_AudioSource.isPlaying) m_AudioSource.Play();
            else if (!isWalking) m_AudioSource.Stop();
        }

        // Only touch rotation while walking. When idle, his rotation stays relative to the
        // marker, so he follows it if tracking adjusts the marker's pose.
        if (isWalking)
        {
            Vector3 desiredForward = Vector3.RotateTowards(
                transform.forward, m_Movement, turnSpeed * Time.deltaTime, 0f);
            transform.rotation = Quaternion.LookRotation(desiredForward, up);
        }
    }

    void OnAnimatorMove()
    {
        // The Walk clip walks in place (no root motion), so by default move at a fixed speed,
        // scaled by the character's size so a shrunk-down Lemon walks at the right pace.
        float step = useRootMotion
            ? m_Animator.deltaPosition.magnitude
            : walkSpeed * transform.lossyScale.x * Time.deltaTime;

        transform.position += m_Movement * step;
    }

    void SetShown(bool shown)
    {
        m_Shown = shown;
        foreach (var r in m_Renderers) r.enabled = shown;
    }
}
