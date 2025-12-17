using UnityEngine;
using Character.Projectiles;

namespace CardSystem.SkillSystem.Execution
{
    /// <summary>
    /// 投射物执行模块：
    /// - 根据 SkillContext（以目标列表或瞄向方向）生成一枚或多枚投射物。
    /// - 若存在目标列表（Targets），则为每个目标生成一枚朝向该目标的投射物（每个目标一枚）。
    /// - 若不存在目标，则生成一枚朝 ctx.AimDirection 发射的投射物。
    /// - 投射物的伤害优先从施法者的属性获取（若可用），否则使用固定覆盖值。
    /// </summary>
    [CreateAssetMenu(menuName = "Card System/Execution/Projectile")]
    public class ProjectileExecutionModuleSO : ExecutionModuleSO
    {
        [Header("投射物")]
        [Tooltip("投射物配置 SO，包含预制体与运动设置")]
        public ProjectileConfig projectileConfig;

        [Tooltip("用于投射物命中检测的 LayerMask（会传给 Projectile.Init）")]
        public LayerMask hitMask;

        [Header("伤害")]
        [Tooltip("若 > 0 则使用此固定伤害作为每个投射物的伤害；否则尝试从施法者属性推导伤害")]
        public float fixedDamage = 0f;

        /// <summary>
        /// 如果为 true，则尝试从拥有者的 CharacterStats.CalculateAttackDamage().Amount 获取伤害值。
        /// </summary>
        [Tooltip("如果为 true，则尝试从拥有者的 CharacterStats.CalculateAttackDamage().Amount 获取伤害值。")]
        public bool useOwnerDamage = true;

        [Header("生成配置")]
        [Tooltip("若为 true 且 ctx.Targets 包含多个有效目标，则为每个目标各发一枚投射物；否则仅朝第一个目标发射一枚")]
        public bool spawnPerTarget = true;

        [Tooltip("从施法者位置偏移的可选生成点")]
        public Vector3 spawnOffset = Vector3.zero;

        public override void Execute(SkillContext ctx)
        {
            if (ctx == null) return;
            if (projectileConfig == null || projectileConfig.projectilePrefab == null)
            {
                Debug.LogWarning("[ProjectileExecutionModuleSO] Missing projectileConfig or prefab.");
                return;
            }

            // 如果 ExplicitTarget 不为空且 Targets 为空，则把 ExplicitTarget 当作单体目标
            if ((ctx.Targets == null || ctx.Targets.Count == 0) && ctx.ExplicitTarget != null)
            {
                ctx.Targets.Add(ctx.ExplicitTarget);
            }

            // 计算基础伤害值
            float baseDamage = fixedDamage;
            if (useOwnerDamage && (baseDamage <= 0f) && ctx.Owner != null)
            {
                var stats = ctx.Owner.GetComponent<Character.Components.CharacterStats>();
                if (stats != null)
                {
                    try
                    {
                        var di = stats.CalculateAttackDamage();
                        baseDamage = di.Amount;
                    }
                    catch { }
                }
            }

            // 确保至少有一个可用的伤害值
            if (baseDamage <= 0f) baseDamage = fixedDamage;

            // 发射点（优先使用施法者的 Combat.FirePoint，如无则使用施法者位置）
            Vector3 firePos = ctx.Position;
            if (ctx.Owner != null)
            {
                var combat = ctx.Owner.GetComponent<Character.Components.CombatComponent>();
                if (combat != null && combat.FirePoint != null)
                {
                    firePos = combat.FirePoint.position;
                }
                else
                {
                    firePos = ctx.Owner.position;
                }
            }
            firePos += spawnOffset;

            // 若存在目标，则朝目标发射投射物
            if (ctx.Targets != null && ctx.Targets.Count > 0)
            {
                if (spawnPerTarget)
                {
                    foreach (var tgt in ctx.Targets)
                    {
                        if (tgt == null) continue;
                        var dir = (tgt.transform.position - firePos);
                        Vector2 aimDir = dir.sqrMagnitude > 0.0001f ? (Vector2)dir.normalized : ctx.AimDirection;
                        SpawnProjectile(firePos, aimDir, baseDamage, ctx);
                    }
                }
                else
                {
                    // 向第一个有效目标发射单枚投射物
                    GameObject first = null;
                    foreach (var t in ctx.Targets) if (t != null) { first = t; break; }
                    if (first != null)
                    {
                        var dir = (first.transform.position - firePos);
                        Vector2 aimDir = dir.sqrMagnitude > 0.0001f ? (Vector2)dir.normalized : ctx.AimDirection;
                        SpawnProjectile(firePos, aimDir, baseDamage, ctx);
                    }
                }
            }
            else
            {
                // 无目标时使用瞄向方向（回退为向右）
                Vector2 aim = ctx.AimDirection;
                if (aim.sqrMagnitude < 0.0001f) aim = Vector2.right;
                SpawnProjectile(firePos, aim.normalized, baseDamage, ctx);
            }
        }

        private void SpawnProjectile(Vector3 position, Vector2 direction, float damage, SkillContext ctx)
        {
            // 使用统一的投射物生成工具（会优先使用对象池，否则实例化），由 ProjectileSpawner 统一管理初始化与错误处理
            var projectile = ProjectileSpawner.Spawn(projectileConfig, position, direction, damage, ctx.OwnerTeam, ctx.Owner, hitMask);
            if (projectile == null) return;
        }
    }
}
