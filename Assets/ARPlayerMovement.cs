using UnityEngine;

/// <summary>
/// AR version of John Lemon's PlayerMovement.
/// Reads input from the on-screen HoldButtons (plus the keyboard in the Editor),
/// drives the "IsWalking" animator bool, and moves with root motion like the original.
/// Movement is camera-relative by default, so "Left" always means left on the screen,
/// whichever side of the marker you're looking from.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class ARPlayerMovement : MonoBehaviour
{
    public float turnSpeed = 20f;

    [Tooltip("On: left/right are relative to the phone camera. Off: relative to the marker.")]
    public bool cameraRelative = true;

    [Tooltip("Recommended in AR: there is usually no floor collider, so gravity would make him fall.")]
    public bool makeKinematic = true;

    Animator m_Animator;
    Rigidbody m_Rigidbody;
    AudioSource m_AudioSource;
    Transform m_Camera;

    Vector3 m_Movement;
    Quaternion m_Rotation;

    void Start()
    {
        m_Animator = GetComponent<Animator>();
        m_Rigidbody = GetComponent<Rigidbody>();
        m_AudioSource = GetComponent<AudioSource>();
        if (Camera.main != null) m_Camera = Camera.main.transform;

        if (makeKinematic)
        {
            m_Rigidbody.isKinematic = true;
            m_Rigidbody.useGravity = false;
        }

        // Keep the rotation he was spawned with (the original starts at identity, which in AR
        // would snap him to face world-forward).
        m_Rotation = transform.rotation;
    }

    void FixedUpdate()
    {
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

        if (isWalking)
        {
            Vector3 desiredForward = Vector3.RotateTowards(
                transform.forward, m_Movement, turnSpeed * Time.deltaTime, 0f);
            m_Rotation = Quaternion.LookRotation(desiredForward, up);
        }
    }

    void OnAnimatorMove()
    {
        // Same as the tutorial: the walk animation's root motion sets the speed. It's already
        // in world scale, so a shrunk-down Lemon walks at the right size.
        m_Rigidbody.MovePosition(m_Rigidbody.position + m_Movement * m_Animator.deltaPosition.magnitude);
        m_Rigidbody.MoveRotation(m_Rotation);
		Debug.Log("root motion: " + m_Animator.deltaPosition.magnitude);
    }
}
