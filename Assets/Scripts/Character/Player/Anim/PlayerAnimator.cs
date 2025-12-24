using Character;
using Character.Components;
using System;
using UnityEngine;

/// <summary>
/// 玩家动画控制器，负责管理玩家的动画状态和过渡
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
    [Header("动画参数")]
    [SerializeField] private float transitionDuration = 0.1f; // 动画过渡时间

    [Header("调试")]
    [SerializeField] private bool enabledebug;

    private Animator animator;
    private PlayerStateMachine stateMachine;
    private CharacterState lastPlayedState = CharacterState.Idle;

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

        // 获取状态机引用
        stateMachine = GetComponent<PlayerStateMachine>();
        if (stateMachine == null)
        {
            CDTU.Utils.CDLogger.LogWarning("PlayerAnimator: 未找到CharacterStateMachine组件，将无法自动同步状态！", gameObject);
        }
    }

    /// <summary>
    /// 启用时初始化
    /// </summary>
    private void OnEnable()
    {
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged += OnStateChanged;
        }
    }

    /// <summary>
    /// 禁用时清理
    /// </summary>
    private void OnDisable()
    {
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged -= OnStateChanged;
        }
    }

    /// <summary>
    /// 状态改变时的回调
    /// </summary>
    /// <param name="oldState">旧状态</param>
    /// <param name="newState">新状态</param>
    private void OnStateChanged(CharacterState oldState, CharacterState newState)
    {
        PlayStateAnimation(newState);
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
    /// 播放指定状态的动画
    /// </summary>
    /// <param name="state">角色状态</param>
    public void PlayStateAnimation(CharacterState state)
    {
        if (animator == null) return;

        // 避免重复播放相同动画
        if (state == lastPlayedState) return;

        string stateName = GetAnimationName(state);

        if (string.IsNullOrEmpty(stateName))
        {
            CDTU.Utils.CDLogger.LogWarning($"PlayerAnimator: 未找到状态 {state} 对应的动画名称！");
            return;
        }

        // 使用CrossFade进行平滑过渡
        animator.CrossFade(stateName, transitionDuration);
        lastPlayedState = state;

        if (enabledebug)
        {
            CDTU.Utils.CDLogger.Log($"PlayerAnimator: 播放状态动画 - {stateName}");
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
    /// 获取状态对应的动画名称
    /// </summary>
    /// <param name="state">角色状态</param>
    /// <returns>动画名称</returns>
    private string GetAnimationName(CharacterState state)
    {
        switch (state)
        {
            case CharacterState.Idle:
                return "Idle";
            case CharacterState.Move:
                return "Move";
            case CharacterState.Attack:
                return "Attack";
            //skill动画由技能系统控制播放
            // case CharacterState.Skill:
            //     return "Skill";
            case CharacterState.Hurt:
                return "Hurt";
            case CharacterState.Stunned:
                return "Stunned";
            case CharacterState.Dead:
                return "Dead";
            default:
                CDTU.Utils.CDLogger.LogWarning($"PlayerAnimator: 未知状态 {state}");
                return null;
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
    /// 检查当前是否在播放指定状态的动画
    /// </summary>
    /// <param name="state">角色状态</param>
    /// <param name="layerIndex">动画层索引，默认为0</param>
    /// <returns>是否在播放指定状态的动画</returns>
    public bool IsPlayingStateAnimation(CharacterState state, int layerIndex = 0)
    {
        string animationName = GetAnimationName(state);
        return IsPlayingAnimation(animationName, layerIndex);
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