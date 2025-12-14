using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    private EnemyCharacter enemyCharacter => GetComponent<EnemyCharacter>();

    /// <summary>
    /// 播放攻击动画（占位实现，未来连接 Animator）
    /// </summary>
    public void PlayAttack()
    {
        // TODO: 使用 Animator 播放攻击动画
        // Example: animator?.SetTrigger("Attack");
        Debug.Log($"[EnemyAnimator] PlayAttack for {gameObject.name}");
    }

    /// <summary>
    /// 播放死亡动画（占位实现）
    /// </summary>
    public void PlayDeath()
    {
        // TODO: 使用 Animator 播放死亡动画
        Debug.Log($"[EnemyAnimator] PlayDeath for {gameObject.name}");
    }









}