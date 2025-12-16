using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    [Header("动画器引用")]
    [Tooltip("敌人的Animator组件")]
    public Animator animator;

    private EnemyCharacter enemyCharacter => GetComponent<EnemyCharacter>();

    /// <summary>
    /// 播放攻击动画
    /// </summary>
    public void PlayAttackAnim()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        else
        {
            Debug.Log($"[EnemyAnimator] PlayAttack for {gameObject.name} (无Animator)");
        }
    }

    /// <summary>
    /// 播放死亡动画
    /// </summary>
    public void PlayDeathAnim()
    {
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
        else
        {
            Debug.Log($"[EnemyAnimator] PlayDeath for {gameObject.name} (无Animator)");
        }
    }

    /// <summary>
    /// 播放受伤动画
    /// </summary>
    public void PlayTakeDamageAnim()
    {
        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
        }
        else
        {
            Debug.Log($"[EnemyAnimator] PlayTakeDamage for {gameObject.name} (无Animator)");
        }
    }

    /// <summary>
    /// 设置移动动画状态
    /// </summary>
    /// <param name="moveSpeed">移动速度</param>
    /// <param name="isMoving">是否在移动</param>
    public void SetMovementAnim(float moveSpeed, bool isMoving)
    {
        if (animator != null)
        {
            animator.SetFloat("MoveSpeed", isMoving ? moveSpeed : 0f);
            animator.SetBool("IsMoving", isMoving);
        }
    }









}