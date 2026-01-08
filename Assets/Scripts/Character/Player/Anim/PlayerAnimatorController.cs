using Character;
using Character.Components;
using System;
using UnityEngine;

/// <summary>
/// 玩家动画控制器，负责管理玩家的动画状态和过渡
/// </summary>
public class PlayerAnimatorController : MonoBehaviour, IAnimatorController
{
    [Header("调试")]
    [SerializeField] private bool enabledebug;

    private Animator animator;

    // 动画参数名称常量
    private static readonly string IS_MOVING_PARAM = "IsMoving";
    private static readonly string ATTACK_TRIGGER = "Attack";
    private static readonly string HURT_TRIGGER = "Hurt";
    private static readonly string DIE_TRIGGER = "Die";

    // 事件
    public event Action OnAttackAnimationStart;
    public event Action OnAttackAnimationEnd;
    public event Action OnHurtAnimationStart;
    public event Action OnDieAnimationStart;

    /// <summary>
    /// 初始化
    /// </summary>
    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            CDTU.Utils.CDLogger.LogError("PlayerAnimator: 未找到Animator组件！", gameObject);
            return;
        }
    }

    /// <summary>
    /// 启用时初始化
    /// </summary>
    private void OnEnable()
    {
    }

    /// <summary>
    /// 禁用时清理
    /// </summary>
    private void OnDisable()
    {
    }

    /// <summary>
    /// 设置移动动画参数
    /// </summary>
    /// <param name="moveVector">移动向量</param>
    /// <param name="isMoving">是否在移动</param>
    public void SetMovement(Vector2 moveVector, bool isMoving)
    {
        if (animator == null) return;

        // 设置移动参数
        animator.SetBool(IS_MOVING_PARAM, isMoving);


        if (enabledebug && isMoving)
        {
            CDTU.Utils.CDLogger.Log($"PlayerAnimator: 设置移动参数 - 方向: {moveVector}, 移动: {isMoving}, 当前动画: {GetCurrentAnimationInfo()}");
        }
    }

    /// <summary>
    /// 播放攻击动画
    /// </summary>
    public void PlayAttack()
    {
        if (animator == null) return;

        animator.SetTrigger(ATTACK_TRIGGER);
        OnAttackAnimationStart?.Invoke();

        if (enabledebug)
        {
            CDTU.Utils.CDLogger.Log("PlayerAnimator: 播放攻击动画");
        }
    }

    /// <summary>
    /// 播放受伤动画
    /// </summary>
    public void PlayHurt()
    {
        if (animator == null) return;

        animator.SetTrigger(HURT_TRIGGER);
        OnHurtAnimationStart?.Invoke();

        if (enabledebug)
        {
            CDTU.Utils.CDLogger.Log("PlayerAnimator: 播放受伤动画");
        }
    }

    /// <summary>
    /// 播放死亡动画
    /// </summary>
    public void PlayDie()
    {
        if (animator == null) return;

        animator.SetTrigger(DIE_TRIGGER);
        OnDieAnimationStart?.Invoke();

        if (enabledebug)
        {
            CDTU.Utils.CDLogger.Log("PlayerAnimator: 播放死亡动画");
        }
    }

    public void PlaySkill(string animationTrigger)
    {
        if (animator == null || string.IsNullOrEmpty(animationTrigger)) return;

        animator.SetTrigger(animationTrigger);

        if (enabledebug)
        {
            CDTU.Utils.CDLogger.Log($"PlayerAnimator: 播放技能动画 - 触发器: {animationTrigger}");
        }
    }

    /// <summary>
    /// 重置所有触发器
    /// </summary>
    public void ResetTriggers()
    {
        if (animator == null) return;

        animator.ResetTrigger(ATTACK_TRIGGER);
        animator.ResetTrigger(HURT_TRIGGER);
        animator.ResetTrigger(DIE_TRIGGER);

        if (enabledebug)
        {
            CDTU.Utils.CDLogger.Log("PlayerAnimator: 重置所有触发器");
        }
    }

    /// <summary>
    /// 获取当前动画状态信息
    /// </summary>
    /// <returns>当前动画状态信息</returns>
    public string GetCurrentAnimationInfo()
    {
        if (animator == null) return "Animator未初始化";

        var animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return $"动画名称哈希: {animatorStateInfo.shortNameHash}, " +
               $"标准化时间: {animatorStateInfo.normalizedTime}, 是否在过渡: {animator.IsInTransition(0)}";
    }

    /// <summary>
    /// 检查当前是否在播放指定动画
    /// </summary>
    /// <param name="stateName">动画状态名称</param>
    /// <param name="layerIndex">动画层索引，默认为0</param>
    /// <returns>是否在播放指定动画</returns>
    public bool IsPlayingAnimation(string stateName, int layerIndex = 0)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return false;

        var animatorStateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
        return animatorStateInfo.IsName(stateName);
    }

    /// <summary>
    /// 攻击动画结束回调（需要在动画事件中调用）
    /// </summary>
    public void OnAttackAnimationEndCallback()
    {
        OnAttackAnimationEnd?.Invoke();

        if (enabledebug)
        {
            CDTU.Utils.CDLogger.Log("PlayerAnimator: 攻击动画结束");
        }
    }
}