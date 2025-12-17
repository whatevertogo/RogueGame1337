using UnityEngine;
using Character.Core;
using Character.Projectiles;

namespace Character.Combat
{
    /// <summary>
    /// 投射物攻击策略 - 支持对象池
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileAttack", menuName = "RogueGame/Combat/Projectile Attack")]
    public class ProjectileAttackStrategy : AttackStrategyBase
    {
        [Header("投射物配置")]
        [Tooltip("是否使用 Context 中的 ProjectileConfig")]
        public bool useContextConfig = true;

        [Tooltip("默认投射物配置")]
        public ProjectileConfig defaultConfig;

        public override void Execute(AttackContext context)
        {
            var config = useContextConfig ? context.ProjectileConfig : defaultConfig;

            if (config == null || config.projectilePrefab == null)
            {
                Debug.LogError("[ProjectileAttackStrategy] 投射物配置无效！");
                return;
            }

            // 计算旋转（用于视觉/朝向展示）
            float angle = Mathf.Atan2(context.AimDirection.y, context.AimDirection.x) * Mathf.Rad2Deg - 90f;
            Quaternion rotation = Quaternion.Euler(0, 0, angle);

            // 生成投射物（统一委托给 ProjectileSpawner，由其处理对象池/实例化与初始化）
            var projectile = ProjectileSpawner.Spawn(
                config,
                context.FirePosition,
                context.AimDirection.normalized,
                context.DamageInfo.Amount,
                context.OwnerTeam,
                context.Owner,
                context.HitMask
            );

            if (projectile == null) return;
        }

        /// <summary>
        /// （弃用）投射物获取：现在建议使用 ProjectileSpawner.Spawn(...) 来统一处理池/实例化/初始化。
        /// 若仍需兼容旧逻辑，此方法作为退路仍保持可用。
        /// </summary>
        private ProjectileBase GetProjectile(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            Debug.LogWarning("[ProjectileAttackStrategy] GetProjectile 已弃用，请使用 ProjectileSpawner.Spawn(...)");
            if (ProjectilePool.Instance != null)
            {
                return ProjectilePool.Instance.Get(prefab, position, rotation);
            }

            var go = Instantiate(prefab, position, rotation);
            return go.GetComponent<ProjectileBase>();
        }

        public override void DrawGizmos(Vector3 position, Vector2 direction)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(position, direction * 3f);
        }
    }
}
