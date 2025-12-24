using System;
using UnityEngine;
using Character.Combat;
using Core;

namespace Character.Components
{
    /// <summary>
    /// 战斗组件 - 使用策略模式处理不同攻击方式
    /// </summary>
    public class CombatComponent : MonoBehaviour
    {
        [Header("攻击策略")]
        [Tooltip("攻击策略 SO（投射物/近战等）")]
        //
        public AttackStrategyBaseSO attackStrategy;

        [Header("投射物配置（远程攻击时使用）")]
        [Tooltip("投射物配置 SO")]
        //
        public ProjectileConfig projectileConfig;

        [Header("攻击设置")]
        [Tooltip("发射点/攻击原点")]
        public Transform FirePoint;

        [Tooltip("可命中的图层")]
        public LayerMask hitMask;

        [Header("调试")]
        [SerializeField] private bool enableDebugLog = false;
        [SerializeField] private bool drawGizmos = true;

        [Tooltip("始终显示可视化（否则仅在调试或需求时显示）")]
        public bool showVisualizer = true;

        // 组件缓存
        private CharacterStats stats;
        private CharacterBase characterBase;
        private StatusEffectComponent statusEffects;
        private Animator anim;

        // 运行时状态
        private float lastAttackTime;
        private Vector2 aimDirection = Vector2.right;

        // ========== 事件 ==========
        public event Action OnAttack;
        public event Action<DamageInfo> OnDamageDealt;

        // ========== 属性 ==========
        public float LastAttackDamage { get; private set; }
        public Vector2 AimDirection => aimDirection;
        public bool CanAttack => !IsOnCooldown && !IsDisabled;
        public bool IsOnCooldown => Time.time < lastAttackTime + AttackCooldown;
        public bool IsDisabled => statusEffects != null &&
            (statusEffects.IsStunned || statusEffects.IsSilenced);

        private float AttackCooldown => 1f / Mathf.Max(0.01f, stats?.AttackSpeed?.Value ?? 1f);

        [Header("普通攻击动画")]
        [SerializeField] private AnimationClip attackClip;



        /// <summary>
        /// 当前使用的攻击策略
        /// </summary>
        public IAttackStrategy CurrentStrategy => attackStrategy;

        // ========== 生命周期 ==========

        private void Awake()
        {
            CacheComponents();
            ValidateSetup();
        }


        private void CacheComponents()
        {
            stats = GetComponent<CharacterStats>();
            statusEffects = GetComponent<StatusEffectComponent>();
            characterBase = GetComponent<CharacterBase>();
            anim = GetComponent<Animator>();
        }

        /// <summary>
        /// 验证组件设置
        /// </summary>
        private void ValidateSetup()
        {
            if (attackStrategy == null)
            {
                Debug.LogWarning($"[CombatComponent] {gameObject.name} 没有设置攻击策略！");
            }

            if (stats == null)
            {
                Debug.LogError($"[CombatComponent] {gameObject.name} 缺少 CharacterStats 组件！");
            }

            // HitMask 可能为空，导致攻击不会命中任何目标，提示开发者检查层设置
            if (hitMask == 0)
            {
                Debug.LogWarning($"[CombatComponent] {gameObject.name} hitMask 为空，这会导致攻击无法命中任何目标（请在 Inspector 设置可命中图层）。");
                return;
            }

            // 如果场景中存在 Enemy，但都不在 hitMask 中，也提示可能的层/掩码错误
            var allChars = ObjectCache.Instance.FindObjectsOfType<CharacterBase>();
            bool anyEnemyExist = false;
            bool anyEnemyInMask = false;
            foreach (var ch in allChars)
            {
                // 跳过已销毁或 null 的对象（防止在编辑器中访问已被销毁的 UnityEngine.Object 抛出 MissingReferenceException）
                if (ch == null) continue;

                if (ch.Team == TeamType.Enemy)
                {
                    anyEnemyExist = true;
                    // 访问 gameObject.layer 前再次确保 ch 未被销毁
                    try
                    {
                        if (((1 << ch.gameObject.layer) & hitMask.value) != 0)
                        {
                            anyEnemyInMask = true;
                            break;
                        }
                    }
                    catch (UnityEngine.MissingReferenceException)
                    {
                        // 被销毁的对象，跳过
                        continue;
                    }
                }
            }
            if (anyEnemyExist && !anyEnemyInMask)
            {
                Debug.LogWarning($"[CombatComponent] {gameObject.name} hitMask 中没有包含任何 Enemy 的图层，请在 Inspector 中检查 Layer 和 HitMask 设置。");
            }
        }

        // ========== 公共 API ==========

        /// <summary>
        /// 设置瞄准方向
        /// </summary>
        public void SetAim(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                aimDirection = direction.normalized;
            }
        }

        /// <summary>
        /// 运行时切换攻击策略
        /// </summary>
        public void SetAttackStrategy(AttackStrategyBaseSO strategy)
        {
            attackStrategy = strategy;

            if (enableDebugLog)
            {
                Debug.Log($"[CombatComponent] 切换攻击策略:  {strategy?.strategyName ?? "null"}");
            }

        }

        /// <summary>
        /// 尝试攻击
        /// </summary>
        /// <returns>是否成功发起攻击</returns>
        public bool TryAttack()
        {
            if (enableDebugLog)
            {
                Debug.Log($"[CombatComponent] TryAttack -> CanAttack: {CanAttack}, Cooldown: {IsOnCooldown}, Disabled: {IsDisabled}");
            }

            // 检查攻击条件
            if (!CanAttack)
                return false;

            if (stats == null)
            {
                Debug.LogError("[CombatComponent] CharacterStats 为空！");
                return false;
            }

            if (attackStrategy == null)
            {
                Debug.LogWarning("[CombatComponent] 没有设置攻击策略！");
                return false;
            }


            // 1. 计算伤害
            DamageInfo damageInfo = CalculateDamage();

            // 2. 记录攻击时间
            LastAttackDamage = damageInfo.Amount;
            lastAttackTime = Time.time;

            // 3. 构建攻击上下文
            AttackContext context = BuildAttackContext(damageInfo);

            // 绑定动画时机（防护：attackClip 或 anim 可能未在 Inspector 中赋值）
            float animLength = attackClip != null ? attackClip.length : AttackCooldown;
            float speed = animLength / Mathf.Max(0.0001f, AttackCooldown);

            if (anim != null)
            {
                anim.SetFloat("AttackSpeed", speed);
            }
            else if (enableDebugLog)
            {
                Debug.LogWarning($"[CombatComponent] {gameObject.name} Animator 未设置，跳过设置 AttackSpeed。");
            }
            if (attackClip == null && enableDebugLog)
            {
                Debug.LogWarning($"[CombatComponent] {gameObject.name} attackClip 未分配，使用 AttackCooldown({AttackCooldown:F3}) 作为动画时长替代。");
            }

            // 4. 执行攻击策略
            attackStrategy.Execute(context);

            if (enableDebugLog)
            {
                Debug.Log($"[CombatComponent] 攻击执行 - 策略: {attackStrategy.strategyName}, 伤害: {damageInfo.Amount}");
            }

            // 5. 触发事件
            OnAttack?.Invoke();
            OnDamageDealt?.Invoke(damageInfo);

            return true;
        }

        /// <summary>
        /// 获取剩余冷却时间
        /// </summary>
        public float GetRemainingCooldown()
        {
            return Mathf.Max(0, (lastAttackTime + AttackCooldown) - Time.time);
        }

        /// <summary>
        /// 获取冷却进度 (0-1)
        /// </summary>
        public float GetCooldownProgress()
        {
            if (AttackCooldown <= 0) return 1f;
            float elapsed = Time.time - lastAttackTime;
            return Mathf.Clamp01(elapsed / AttackCooldown);
        }

        /// <summary>
        /// 重置冷却
        /// </summary>
        public void ResetCooldown()
        {
            lastAttackTime = -AttackCooldown;
        }

        // ========== 内部方法 ==========

        private DamageInfo CalculateDamage()
        {
            // 从 CharacterStats 计算基础伤害
            DamageInfo damageInfo = stats.CalculateAttackDamage();

            // 应用状态效果的伤害修改
            if (statusEffects != null)
            {
                damageInfo.Amount = statusEffects.ModifyOutgoingDamage(damageInfo.Amount);
            }

            // 设置伤害来源
            damageInfo.Source = gameObject;

            return damageInfo;
        }

        private AttackContext BuildAttackContext(DamageInfo damageInfo)
        {
            return new AttackContext
            {
                Owner = transform,
                OwnerTeam = characterBase != null ? characterBase.Team : TeamType.Neutral,
                AimDirection = aimDirection,
                FirePosition = FirePoint != null ? FirePoint.position : transform.position,
                DamageInfo = damageInfo,
                HitMask = hitMask,
                ProjectileConfig = projectileConfig
            };
        }

        // ========== Gizmos ==========

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            Vector3 pos = FirePoint != null ? FirePoint.position : transform.position;

            // 绘制发射点
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pos, 0.1f);

            // 绘制瞄准方向
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(pos, aimDirection * 1.5f);

            // 让策略绘制自己的 Gizmos
            attackStrategy?.DrawGizmos(pos, aimDirection);
        }
#endif
    }
}