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

            // 计算旋转
            float angle = Mathf.Atan2(context.AimDirection.y, context.AimDirection.x) * Mathf.Rad2Deg - 90f;
            Quaternion rotation = Quaternion.Euler(0, 0, angle);

            // 获取投射物（优先使用对象池）
            ProjectileBase projectile = GetProjectile(config.projectilePrefab, context.FirePosition, rotation);

            if (projectile != null)
            {
                projectile.Init(
                    config,
                    context.AimDirection,
                    context.DamageInfo.Amount,
                    context.OwnerTeam,
                    context.Owner,
                    context.HitMask
                );
            }
        }

        /// <summary>
        /// 获取投射物（对象池或实例化）
        /// </summary>
        private ProjectileBase GetProjectile(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            // 优先使用对象池
            if (ProjectilePool.Instance != null)
            {
                return ProjectilePool.Instance.Get(prefab, position, rotation);
            }

            // 回退：直接实例化
            var go = Instantiate(prefab, position, rotation);
            var projectile = go.GetComponent<ProjectileBase>();

            if (projectile == null)
            {
                Debug.LogError($"[ProjectileAttackStrategy] 预制体 {prefab.name} 缺少 ProjectileBase！");
                Destroy(go);
                return null;
            }

            return projectile;
        }

        public override void DrawGizmos(Vector3 position, Vector2 direction)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(position, direction * 3f);
        }
    }
}