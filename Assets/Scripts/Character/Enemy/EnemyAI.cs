using UnityEngine;
using Character.Components;
using System.Collections;

/// <summary>
/// 通用敌人 AI 基类
/// 负责：目标感知 / 状态调度 / 移动与攻击调用
/// 不负责：具体攻击形式、特殊行为
/// </summary>
[RequireComponent(typeof(MovementComponent))]
[RequireComponent(typeof(CombatComponent))]
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(EnemyAnimator))]
public class EnemyAI : MonoBehaviour
{
    private static readonly WaitForSeconds HitStunWait = new WaitForSeconds(0.1f);

    #region Components

    protected MovementComponent movement;
    protected CombatComponent combat;
    protected HealthComponent health;
    protected CharacterStats stats;
    protected EnemyAnimator enemyAnimator;

    protected Transform target;

    #endregion

    #region Inspector

    [Header("AI 基础")]
    [Tooltip("无目标时的移动速度（0 表示原地待机）")]
    public float idleSpeed = 0f;

    [Tooltip("仇恨距离（0 表示无限）")]
    public float aggroRange = 10f;

    [Tooltip("AI Tick 间隔（0 表示每帧）")]
    public float updateInterval = 0.05f;

    [Header("战斗")]
    public float attackRange = 0f;
    public float attackCooldown = 0f;

    [Header("死亡表现")]
    public bool playDeathAnimation = true;
    public GameObject deathEffect;

    #endregion

    #region State

    protected bool isDead;
    protected bool isStunned;

    private float _lastAttackTime;
    private float _lastUpdateTime;

    private float _nextTargetSearchTime;
    private Vector2 _idleDirection;
    private float _nextIdleDirTime;

    #endregion

    #region Unity

    protected virtual void Awake()
    {
        movement = GetComponent<MovementComponent>();
        combat = GetComponent<CombatComponent>();
        health = GetComponent<HealthComponent>();
        stats = GetComponent<CharacterStats>();
        enemyAnimator = GetComponent<EnemyAnimator>();

        if (stats != null)
        {
            stats.OnDeath += HandleDeath;
            stats.OnDamaged += HandleDamaged;
        }
    }

    protected virtual void Start()
    {
        InitCombatParams();
        TryFindTarget();
    }

    protected virtual void Update()
    {
        if (isDead) return;

        if (updateInterval > 0f && Time.time < _lastUpdateTime + updateInterval)
            return;

        _lastUpdateTime = Time.time;
        TickAI();
    }

    protected virtual void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnDeath -= HandleDeath;
            stats.OnDamaged -= HandleDamaged;
        }
    }

    #endregion

    #region AI Core

    protected virtual void TickAI()
    {
        if (target == null)
        {
            TryFindTarget();
            if (target == null)
            {
                TickIdle();
                return;
            }
        }

        if (isStunned)
        {
            movement.SetInput(Vector2.zero);
            return;
        }

        Vector2 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        if (aggroRange > 0f && distance > aggroRange)
        {
            TickIdle();
            return;
        }

        TickCombat(toTarget, distance);
    }

    protected virtual void TickCombat(Vector2 toTarget, float distance)
    {
        Vector2 dir = toTarget.normalized;

        if (distance <= attackRange && Time.time >= _lastAttackTime + attackCooldown)
        {
            TryAttack(dir);
        }
        else if (distance > attackRange)
        {
            MoveTowards(dir);
        }
        else
        {
            movement.SetInput(Vector2.zero);
            combat.SetAim(dir);
        }
    }

    #endregion

    #region Behavior

    protected virtual void TryAttack(Vector2 direction)
    {
        movement.SetInput(Vector2.zero);
        combat.SetAim(direction);

        if (combat.TryAttack())
        {
            _lastAttackTime = Time.time;
            OnAttackPerformed();
        }
    }

    protected virtual void MoveTowards(Vector2 direction)
    {
        movement.SetInput(direction);
        combat.SetAim(direction);

        enemyAnimator?.SetMovementAnim(true);
    }

    protected virtual void TickIdle()
    {
        if (idleSpeed <= 0f)
        {
            movement.SetInput(Vector2.zero);
            enemyAnimator?.SetMovementAnim(false);
            return;
        }

        if (Time.time >= _nextIdleDirTime)
        {
            _idleDirection = Random.insideUnitCircle.normalized;
            _nextIdleDirTime = Time.time + 1.5f;
        }

        movement.SetInput(_idleDirection * idleSpeed);
        enemyAnimator?.SetMovementAnim(true);
    }

    #endregion

    #region Target

    protected virtual void TryFindTarget()
    {
        if (Time.time < _nextTargetSearchTime) return;

        _nextTargetSearchTime = Time.time + 0.5f;
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            target = player.transform;
        }
    }

    #endregion

    #region Combat Params

    protected virtual void InitCombatParams()
    {
        if (stats == null) return;

        if (attackRange <= 0f && stats.AttackRange != null)
            attackRange = stats.AttackRange.Value;

        if (attackCooldown <= 0f && stats.AttackSpeed != null)
            attackCooldown = 1f / Mathf.Max(0.01f, stats.AttackSpeed.Value);
    }

    #endregion

    #region Damage & Death

    protected virtual void HandleDamaged(float damage)
    {
        enemyAnimator?.PlayTakeDamageAnim();

        if (!isStunned)
            StartCoroutine(HitStunRoutine());
    }

    protected virtual IEnumerator HitStunRoutine()
    {
        isStunned = true;
        yield return HitStunWait;
        isStunned = false;
    }

    protected virtual void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        movement.SetInput(Vector2.zero);

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        StartCoroutine(DeathRoutine());
    }

    protected virtual IEnumerator DeathRoutine()
    {
        if (playDeathAnimation)
        {
            float t = 0f;
            float duration = 0.4f;
            Vector3 start = transform.localScale;

            while (t < duration)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(start, Vector3.zero, t / duration);
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    #endregion

    #region Hooks

    /// <summary>
    /// 攻击成功回调（子类扩展）
    /// </summary>
    protected virtual void OnAttackPerformed() { }

    #endregion

    #region Gizmos

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

    #endregion
}
