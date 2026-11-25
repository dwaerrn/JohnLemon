using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterWalker : MonoBehaviour
{
    public float moveSpeed = 0.15f;   // world units/sec - tune to your scale
    public float turnSpeed = 120f;    // degrees/sec

    Animator animator;
    float move;   // -1..1
    float turn;   // -1..1

    void Awake()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false; // we move via script
    }

    public void SetMove(float value) => move = value;
    public void SetTurn(float value) => turn = value;

    void Update()
    {
        transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f, Space.Self);
        transform.Translate(Vector3.forward * move * moveSpeed * Time.deltaTime, Space.Self);

        animator.SetBool("IsWalking", Mathf.Abs(move) > 0.01f);
    }
}