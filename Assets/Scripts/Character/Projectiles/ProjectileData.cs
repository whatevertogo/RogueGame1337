using UnityEngine;

namespace Character.Projectiles
{
    /// <summary>
    /// 投射物运行时数据（Init时传入）
    /// </summary>
    public struct ProjectileData
    {
        // 来源信息
        public Transform Owner;
        public TeamType OwnerTeam;

        // 伤害信息
        public float Damage;
        // public bool IsCrit;

        // 运动信息
        public Vector2 Direction;
        public float Speed;
        public float Lifetime;
        public int PierceCount;

        // 碰撞信息
        public LayerMask HitMask;

        // 追踪信息
        public bool IsHoming;
        public float HomingStrength;
        public float HomingRadius;

        // 特效
        public GameObject HitEffect;

        /// <summary>
        /// 从配置创建运行时数据
        /// </summary>
        public static ProjectileData FromConfig(
            ProjectileConfig config,
            Vector2 direction,
            float damage,
            TeamType ownerTeam,
            Transform owner,
            LayerMask hitMask,
            bool isCrit = false)
        {
            return new ProjectileData
            {
                Owner = owner,
                OwnerTeam = ownerTeam,
                Damage = damage * config.damageMultiplier,
                // IsCrit = isCrit,
                Direction = direction.normalized,
                Speed = config.speed,
                Lifetime = config.lifetime,
                PierceCount = config.pierceCount,
                HitMask = hitMask,
                IsHoming = config.isHoming,
                HomingStrength = config.homingStrength,
                HomingRadius = config.homingRadius,
                HitEffect = config.hitEffect
            };
        }
    }
}