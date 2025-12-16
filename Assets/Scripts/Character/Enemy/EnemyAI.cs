using UnityEngine;
using Character.Components;
using System.Collections;
using Character.Core;

/// <summary>
/// 增强敌人 AI 基类：提供通用的敌人行为框架
/// 支持状态管理、玩家检测、死亡处理等核心功能
/// </summary>
[RequireComponent(typeof(MovementComponent))]
[RequireComponent(typeof(Character.Components.CombatComponent))]
public class EnemyAI : MonoBehaviour
{
    protected MovementComponent movement;
    protected Character.Components.CombatComponent combat;
    protected Character.Components.CharacterStats stats;
    protected Transform target;
    protected HealthComponent health;
    protected EnemyAnimator enemyAnimator;

    [Header("AI 基础设置")]
    [Tooltip("当找不到玩家时的巡逻速度（可为0）")]
    public float idleSpeed = 0f;

    [Tooltip("寻找玩家的最大距离，超过将不追逐（0 表示无限）")]
    public float aggroRange = 0f;

    [Tooltip("AI更新间隔（秒），0表示每帧更新")]
    public float updateInterval = 0f;

    [Header("战斗设置")]
    [Tooltip("攻击范围（如未设置则使用角色属性）")]
    public float attackRange = 0f;

    [Tooltip("攻击冷却时间（如未设置则使用角色属性）")]
    public float attackCooldown = 0f;

    [Header("状态效果")]
    [Tooltip("死亡时是否播放死亡动画")]
    public bool playDeathAnimation = true;

    [Tooltip("死亡特效预制体")]
    public GameObject deathEffect;

    // 状态管理
    protected bool isDead = false;
    protected bool isStunned = false;
    protected float lastAttackTime;
    protected float lastUpdateTime;

    // 事件
    public System.Action OnDeath;
    public System.Action<float> OnDamaged;

    protected virtual void Awake()
    {
        movement = GetComponent<MovementComponent>();
        combat = GetComponent<Character.Components.CombatComponent>();
        stats = GetComponent<Character.Components.CharacterStats>();
        health = GetComponent<HealthComponent>();
        enemyAnimator = GetComponent<EnemyAnimator>();

        // 订阅生命值事件
        if (health != null && stats != null)
        {
            health.OnDeath += HandleDeath;
            health.OnDamaged += HandleDamaged;
        }
    }

    protected virtual void Start()
    {
        // 优先使用场景中的本地玩家（单人场景）
        FindTarget();
        
        // 初始化攻击参数
        if (attackRange <= 0f && stats != null && stats.AttackRange != null)
        {
            attackRange = stats.AttackRange.Value;
        }
        
        if (attackCooldown <= 0f && stats != null && stats.AttackSpeed != null)
        {
            attackCooldown = 1f / stats.AttackSpeed.Value; // 攻击速度转换为冷却时间
        }
    }

    protected virtual void Update()
    {
        if (isDead) return;

        // 根据间隔更新AI逻辑
        if (updateInterval > 0f)
        {
            if (Time.time >= lastUpdateTime + updateInterval)
            {
                UpdateAI();
                lastUpdateTime = Time.time;
            }
        }
        else
        {
            UpdateAI();
        }
    }

    protected virtual void UpdateAI()
    {
        if (stats == null || movement == null || combat == null) return;

        // 尝试查找玩家目标
        if (target == null)
        {
            FindTarget();
            if (target == null)
            {
                // 无玩家：闲置或巡逻
                HandleIdleState();
                return;
            }
        }

        // 检查是否处于控制状态
        if (isStunned)
        {
            movement.SetInput(Vector2.zero);
            return;
        }

        Vector2 toPlayer = (target.position - transform.position);
        float dist = toPlayer.magnitude;

        // 距离判断：若设置了 aggroRange 且超过则不追
        if (aggroRange > 0f && dist > aggroRange)
        {
            HandleIdleState();
            return;
        }

        // 执行战斗或移动逻辑
        HandleCombatState(toPlayer, dist);
    }

    /// <summary>
    /// 处理闲置状态
    /// </summary>
    protected virtual void HandleIdleState()
    {
        if (idleSpeed > 0f)
        {
            // 简单的巡逻逻辑（子类可重写）
            Vector2 randomDirection = new Vector2(
                Mathf.PerlinNoise(Time.time * 0.5f, 0) * 2 - 1,
                Mathf.PerlinNoise(0, Time.time * 0.5f) * 2 - 1
            ).normalized;
            
            movement.SetInput(randomDirection * idleSpeed);
        }
        else
        {
            movement.SetInput(Vector2.zero);
        }
    }

    /// <summary>
    /// 处理战斗状态
    /// </summary>
    protected virtual void HandleCombatState(Vector2 toPlayer, float distance)
    {
        // 如果在攻击范围内，停止移动并攻击；否则移动靠近
        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            HandleAttack(toPlayer.normalized);
        }
        else if (distance > attackRange)
        {
            HandleMovement(toPlayer.normalized);
        }
        else
        {
            // 在攻击范围内但冷却中
            movement.SetInput(Vector2.zero);
            combat.SetAim(toPlayer.normalized);
        }
    }

    /// <summary>
    /// 处理攻击逻辑
    /// </summary>
    protected virtual void HandleAttack(Vector2 direction)
    {
        movement.SetInput(Vector2.zero);
        combat.SetAim(direction);
        
        // 尝试发起攻击（CombatComponent 会处理冷却）
        if (combat.TryAttack())
        {
            lastAttackTime = Time.time;
            OnAttackPerformed();
        }
    }

    /// <summary>
    /// 处理移动逻辑
    /// </summary>
    protected virtual void HandleMovement(Vector2 direction)
    {
        movement.SetInput(direction);
        combat.SetAim(direction);

        // 更新移动动画
        if (enemyAnimator != null && stats != null)
        {
            bool isMoving = direction.sqrMagnitude > 0.01f;
            enemyAnimator.SetMovementAnim(stats.MoveSpeed.Value, isMoving);
        }
    }

    /// <summary>
    /// 查找玩家目标
    /// </summary>
    protected virtual void FindTarget()
    {
        var p = FindObjectOfType<PlayerController>();
        if (p != null) target = p.transform;
    }

    /// <summary>
    /// 攻击执行回调（子类可重写）
    /// </summary>
    protected virtual void OnAttackPerformed()
    {
        // 子类可添加攻击特效、音效等
    }

    /// <summary>
    /// 处理受到伤害
    /// </summary>
    protected virtual void HandleDamaged(DamageResult damageResult)
    {
        OnDamaged?.Invoke(damageResult.FinalDamage);

        // 播放受伤动画
        if (enemyAnimator != null)
        {
            enemyAnimator.PlayTakeDamageAnim();
        }

        // 受伤时的反应（如短暂停顿）
        if (!isStunned)
        {
            StartCoroutine(HitStunRoutine());
        }
    }

    /// <summary>
    /// 处理死亡
    /// </summary>
    protected virtual void HandleDeath()
    {
        if (isDead) return;
        
        isDead = true;
        OnDeath?.Invoke();
        
        // 停止所有移动和攻击
        if (movement != null)
            movement.SetInput(Vector2.zero);
        
        // 播放死亡特效
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        // 延迟销毁对象
        StartCoroutine(DeathRoutine());
    }

    /// <summary>
    /// 受击硬直协程
    /// </summary>
    protected virtual IEnumerator HitStunRoutine()
    {
        isStunned = true;
        yield return new WaitForSeconds(0.1f); // 100ms硬直
        isStunned = false;
    }

    /// <summary>
    /// 死亡处理协程
    /// </summary>
    protected virtual IEnumerator DeathRoutine()
    {
        if (playDeathAnimation)
        {
            // 简单的死亡动画（子类可重写）
            float duration = 0.5f;
            Vector3 startScale = transform.localScale;
            
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float progress = t / duration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
                yield return null;
            }
        }
        
        // 通知房间控制器敌人已死亡
        var roomController = GetComponentInParent<RogueGame.Map.RoomController>();
        if (roomController != null)
        {
            roomController.OnEnemyDeath(this.gameObject);
        }
        
        Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        // 清理事件订阅
        if (health != null)
        {
            health.OnDeath -= HandleDeath;
            health.OnDamaged -= HandleDamaged;
        }
    }

    // 可视化调试
    protected virtual void OnDrawGizmosSelected()
    {
        if (attackRange > 0f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
        
        if (aggroRange > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, aggroRange);
        }
    }
}
