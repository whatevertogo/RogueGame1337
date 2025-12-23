using System.Collections;
using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    [Header("动画器引用")]
    [Tooltip("敌人的Animator组件")]
    public Animator animator;

    [Header("死亡缩放")]
    [Tooltip("死亡时缩小所用时间(秒)")]
    public float deathShrinkDuration = 1f;
    [Tooltip("死亡时目标缩放，通常为Vector3.zero")]
    public Vector3 deathTargetScale = Vector3.zero;

    private Coroutine shrinkCoroutine;

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

        // 开始缩小协程（无论是否有Animator都执行）
        if (shrinkCoroutine != null)
        {
            StopCoroutine(shrinkCoroutine);
            shrinkCoroutine = null;
        }
        shrinkCoroutine = StartCoroutine(ShrinkLocalScale(deathShrinkDuration, deathTargetScale));
    }

    private IEnumerator ShrinkLocalScale(float duration, Vector3 targetScale)
    {
        var tr = transform;
        Vector3 start = tr.localScale;
        if (duration <= 0f)
        {
            tr.localScale = targetScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            tr.localScale = Vector3.Lerp(start, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        tr.localScale = targetScale;
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
    public void SetMovementAnim( bool isMoving)
    {
        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
        }
    }









}