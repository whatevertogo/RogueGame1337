using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void SetMovement(Vector2 moveVector, bool isMoving, bool isRunning)
    {
        animator.SetFloat("moveX", moveVector.x);
        animator.SetFloat("moveY", moveVector.y);
        animator.SetBool("isMoving", isMoving);
        animator.SetBool("isRunning", isRunning);
    }

    public void PlayAttack()
    {
        if (animator == null) return;
        // animator.SetTrigger("attack");
    }
}