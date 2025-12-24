using UnityEngine;
using System.Collections.Generic;
using Character.Components;
using Character;
using Character.Projectiles;

/// <summary>
/// 投射物基类 - 支持对象池
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ProjectileBase : MonoBehaviour
{
    // ========== 缓存组件 ==========
    protected Rigidbody2D rb;
    protected Collider2D col;
    protected TrailRenderer trailRenderer;

    // ========== 运行时数据 ==========
    protected ProjectileData data;
    protected float timer;
    protected int currentPierceCount;
    protected bool isInitialized;
    protected Vector2 currentDirection;

    // ========== 追踪优化 ==========
    protected Transform homingTarget;
    protected float targetSearchTimer;
    protected const float TARGET_SEARCH_INTERVAL = 0.2f;

    // ========== 避免重复命中 ==========
    // 优先使用 HealthComponent 来判断是否重复命中同一实体
    protected HashSet<HealthComponent> hitHealthTargets;
    // 对于没有 HealthComponent 的阻挡物等，使用 instanceId 做退路
    protected HashSet<int> hitInstanceIds;

    private ContactFilter2D HitFilter;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = false;



    /// <summary>
    /// 投射物的拥有者
    /// </summary>
    public Transform Owner => data.Owner;

    // ========== 静态缓冲区（避免 GC）==========
    private static readonly Collider2D[] s_hitBuffer = new Collider2D[16];

    #region 生命周期

    protected virtual void Awake()
    {
        // 缓存组件（只执行一次）
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        trailRenderer = GetComponentInChildren<TrailRenderer>();

        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if (col == null)
        {
            Debug.LogWarning($"[ProjectileBase] {gameObject.name} 缺少 Collider2D！无法检测碰撞。");
        }

        hitHealthTargets = new HashSet<HealthComponent>();
        hitInstanceIds = new HashSet<int>();

        if (enableDebugLog)
        {
            Debug.Log($"[ProjectileBase] Awake: rb.bodyType={(rb != null ? rb.bodyType.ToString() : "null")}, colliderIsTrigger={(col != null ? col.isTrigger.ToString() : "null")}");
        }

        HitFilter = new ContactFilter2D();
        HitFilter.SetLayerMask(data.HitMask);
        HitFilter.useTriggers = true;
    }

    protected virtual void OnEnable()
    {
        // 对象池取出时重置拖尾
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }

    protected virtual void OnDisable()
    {
        // 对象池归还时重置状态
        ResetState();
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化投射物
    /// </summary>
    public virtual void Init(ProjectileData projectileData)
    {
        data = projectileData;
        currentDirection = data.Direction.sqrMagnitude > 0.001f
            ? data.Direction.normalized
            : Vector2.up;
        currentPierceCount = data.PierceCount;
        timer = 0f;
        targetSearchTimer = 0f;
        homingTarget = null;
        hitHealthTargets.Clear();
        hitInstanceIds.Clear();
        isInitialized = true;

        // 设置初始速度
        if (rb != null)
        {
            rb.linearVelocity = currentDirection * data.Speed;
        }

        // 设置旋转
        UpdateRotation();

        if (enableDebugLog)
        {
            string ownerName = data.Owner != null ? data.Owner.name : "null";
            Debug.Log($"[ProjectileBase] Init - Owner={ownerName}, OwnerTeam={data.OwnerTeam}, HitMask.value={data.HitMask.value}, Damage={data.Damage}");
        }
    }

    /// <summary>
    /// 便捷初始化方法（兼容旧代码）
    //TODO-在这里添加暴击，但我不想有暴击后面可以更改如果需要
    /// </summary>
    public void Init(ProjectileConfig config, Vector2 direction, float damage,
        TeamType ownerTeam, Transform owner, LayerMask hitMask, StatusEffectDefinitionSO[] effects = null, bool isCrit = false)
    {
        var projectileData = ProjectileData.FromConfig(
            config, direction, damage, ownerTeam, owner, hitMask, effects);
        Init(projectileData);

        if (enableDebugLog)
        {
            Debug.Log($"[ProjectileBase] Init (compat) - prefab based init, owner={(owner != null ? owner.name : "null")}, ownerTeam={ownerTeam}, hitMask={hitMask.value}");
        }
    }

    /// <summary>
    /// 重置状态
    /// </summary>
    protected virtual void ResetState()
    {
        isInitialized = false;
        homingTarget = null;
        hitHealthTargets?.Clear();
        hitInstanceIds?.Clear();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    #endregion

    #region 更新

    protected virtual void Update()
    {
        if (!isInitialized) return;

        // 生命周期
        timer += Time.deltaTime;
        if (timer >= data.Lifetime)
        {
            OnLifetimeEnd();
            return;
        }

        // 追踪逻辑
        if (data.IsHoming)
        {
            UpdateHoming();
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!isInitialized || rb == null) return;

        rb.linearVelocity = currentDirection * data.Speed;
    }

    protected void UpdateRotation()
    {
        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    #endregion

    #region 追踪

    protected virtual void UpdateHoming()
    {
        targetSearchTimer += Time.deltaTime;

        // 降低搜索频率
        if (targetSearchTimer >= TARGET_SEARCH_INTERVAL)
        {
            targetSearchTimer = 0f;

            if (homingTarget == null || !IsValidHomingTarget(homingTarget))
            {
                homingTarget = FindHomingTarget();
            }
        }

        if (homingTarget == null) return;

        // 平滑转向
        Vector2 toTarget = ((Vector2)homingTarget.position - (Vector2)transform.position).normalized;
        currentDirection = Vector2.Lerp(currentDirection, toTarget,
            data.HomingStrength * Time.deltaTime).normalized;

        UpdateRotation();
    }

    protected virtual Transform FindHomingTarget()
    {
        if (data.HomingRadius <= 0) return null;

        // 使用 NonAlloc 避免 GC
        int hitCount = Physics2D.OverlapCircle(
            transform.position, data.HomingRadius,HitFilter,s_hitBuffer);

        Transform bestTarget = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            var hit = s_hitBuffer[i];
            if (hit == null) continue;

            // 跳过自己
            if (data.Owner != null && hit.transform == data.Owner)
                continue;

            // 跳过同阵营
            var charBase = hit.GetComponent<CharacterBase>();
            if (charBase != null && charBase.Team == data.OwnerTeam)
                continue;

            // 跳过已死亡
            var health = hit.GetComponent<HealthComponent>();
            if (health != null && health.IsDead)
                continue;

            // 计算评分（距离 + 方向权重）
            Vector2 toTarget = (Vector2)hit.transform.position - (Vector2)transform.position;
            float distance = toTarget.magnitude;
            float dot = Vector2.Dot(currentDirection, toTarget.normalized);

            // 优先前方目标
            float score = distance * (dot > 0 ? 1f : 2f);

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = hit.transform;
            }
        }

        return bestTarget;
    }

    protected bool IsValidHomingTarget(Transform target)
    {
        if (target == null) return false;

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > data.HomingRadius * 1.5f) return false;

        var health = target.GetComponent<HealthComponent>();
        return health == null || !health.IsDead;
    }

    #endregion

    #region 碰撞处理

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (enableDebugLog) Debug.Log($"[ProjectileBase] OnTriggerEnter2D collided with {other?.gameObject?.name} (layer={LayerMask.LayerToName(other.gameObject.layer)})");
        TryHit(other.gameObject);
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (enableDebugLog) Debug.Log($"[ProjectileBase] OnCollisionEnter2D collided with {collision?.gameObject?.name} (layer={LayerMask.LayerToName(collision.gameObject.layer)})");

        // 如果是墙壁，直接销毁（不调用 TryHit，避免双重回收）
        if (collision.gameObject.CompareTag("Wall"))
        {
            SpawnHitEffect();
            OnHitDestroy();
            return;
        }

        // 其他物体调用正常命中逻辑
        TryHit(collision.gameObject);
    }

    bool IsAlreadyHit(GameObject target, out HealthComponent health)
    {
        health = target.GetComponent<HealthComponent>();
        if (health != null)
        {
            return hitHealthTargets.Contains(health);
        }
        else
        {
            int targetId = target.GetInstanceID();
            return hitInstanceIds.Contains(targetId);
        }
    }

    protected virtual void TryHit(GameObject target)
    {
        if (!isInitialized) return;

        if (IsAlreadyHit(target, out var health)) return;

        // 图层过滤
        bool inMask = IsInLayerMask(target.layer, data.HitMask);
        if (enableDebugLog && !inMask)
        {
            Debug.Log($"[ProjectileBase] TryHit: target={target.name}, layer={LayerMask.LayerToName(target.layer)}, inHitMask={inMask}");
        }
        if (!inMask) return;



        // 跳过发射者
        if (data.Owner != null && target.transform == data.Owner)
        {
            if (enableDebugLog) Debug.Log("[ProjectileBase] TryHit: skipping owner");
            return;
        }

        // 阵营过滤
        var charBase = target.GetComponent<CharacterBase>();
        if (charBase != null && charBase.Team == data.OwnerTeam)
        {
            if (enableDebugLog) Debug.Log($"[ProjectileBase] TryHit: skipping same team (targetTeam={charBase.Team}, ownerTeam={data.OwnerTeam})");
            return;
        }

        // 标记已命中
        if (health != null)
        {
            hitHealthTargets.Add(health);
        }
        else
        {
            int targetId = target.GetInstanceID();
            hitInstanceIds.Add(targetId);
        }

        // 造成伤害
        if (health != null)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[ProjectileBase] DealDamage -> target={target.name}, healthExists=true, damage={data.Damage}");
            }
            DealDamage(health);
        }

        // 如果投射物携带状态效果定义，尝试将这些效果应用到目标（若目标有 StatusEffectComponent）
        if (data.Effects != null && data.Effects.Length > 0)
        {
            var effectComp = target.GetComponent<Character.Components.StatusEffectComponent>();
            if (effectComp != null)
            {
                foreach (var def in data.Effects)
                {
                    if (def == null) continue;
                    var inst = def.CreateInstance();
                    if (inst == null) continue;
                    effectComp.AddEffect(inst);
                }
            }
        }

        // 在探测后输出物理层忽略信息（仅调试）
        if (enableDebugLog)
        {
            int projectileLayer = gameObject.layer;
            int targetLayer = target.layer;
            bool ignored = Physics2D.GetIgnoreLayerCollision(projectileLayer, targetLayer);
            var col2d = target.GetComponent<Collider2D>();
            Debug.Log($"[ProjectileBase] After TryHit: projectileLayer={LayerMask.LayerToName(projectileLayer)}, targetLayer={LayerMask.LayerToName(targetLayer)}, ignoredByPhysicsMatrix={ignored}, targetColliderIsTrigger={(col2d != null ? col2d.isTrigger : false)}");
        }

        // 穿透处理
        if (currentPierceCount > 0)
        {
            currentPierceCount--;
            OnPierce(target);

            // 追踪弹命中后重新寻找目标
            if (data.IsHoming && homingTarget == target.transform)
            {
                homingTarget = null;
            }
            return;
        }

        // 命中销毁
        SpawnHitEffect();
        OnHitDestroy();
    }

    protected virtual void DealDamage(HealthComponent healthComponent)
    {
        var damageInfo = new DamageInfo
        {
            Amount = data.Damage,
            // IsCrit = data.IsCrit,
            Source = data.Owner?.gameObject,
            HitPoint = transform.position,
            KnockbackDir = currentDirection,
            KnockbackForce = 0
        };

        healthComponent.TakeDamage(damageInfo);
    }

    #endregion

    #region 生命周期回调

    protected virtual void OnPierce(GameObject target)
    {
        // 子类可重写
    }

    protected virtual void OnHitDestroy()
    {
        ReturnToPool();
    }

    protected virtual void OnLifetimeEnd()
    {
        ReturnToPool();
    }

    protected virtual void SpawnHitEffect()
    {
        if (data.HitEffect != null)
        {
            var effect = Instantiate(data.HitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }

    /// <summary>
    /// 归还到对象池
    /// </summary>
    protected virtual void ReturnToPool()
    {
        isInitialized = false;

        if (ProjectilePool.Instance != null)
        {
            ProjectilePool.Instance.Return(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region 工具方法

    protected bool IsInLayerMask(int layer, LayerMask mask)
    {
        return ((1 << layer) & mask.value) != 0;
    }

    #endregion




}