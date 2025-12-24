using UnityEngine;
using Character.Projectiles;

namespace Character.Combat
{
    /// <summary>
    /// 投射物攻击策略 - 支持对象池
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileAttack", menuName = "RogueGame/Combat/Projectile Attack")]
    public class ProjectileAttackStrategy : AttackStrategyBaseSO
    {
        [Header("投射物配置")]
        [Tooltip("是否使用 Context 中的 ProjectileConfig")]
        public bool useContextConfig = true;

        [Tooltip("默认投射物配置")]
        public ProjectileConfig defaultConfig;

        public override void Execute(AttackContext context)
        {
            if (defaultConfig == null || defaultConfig.projectilePrefab == null)
            {
                Debug.LogError("[ProjectileAttackStrategy] 投射物配置无效！");
                return;
            }

            // 计算旋转（用于视觉/朝向展示）
            float angle = Mathf.Atan2(context.AimDirection.y, context.AimDirection.x) * Mathf.Rad2Deg - 90f;
            Quaternion rotation = Quaternion.Euler(0, 0, angle);

            // 生成投射物（统一委托给 ProjectileSpawner，由其处理对象池/实例化与初始化）
            var projectile = ProjectileSpawner.Spawn(
                defaultConfig,
                context.FirePosition,
                context.AimDirection.normalized,
                context.DamageInfo.Amount,
                context.OwnerTeam,
                context.Owner,
                context.HitMask
            );

            if (projectile == null) return;
        }

        public override void DrawGizmos(Vector3 position, Vector2 direction)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(position, direction * 3f);
        }
    }
}
