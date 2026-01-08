using UnityEngine;

namespace Character.Combat
{
    /// <summary>
    /// 攻击策略接口
    /// </summary>
    public interface IAttackStrategy
    {
        /// <summary>
        /// 执行攻击
        /// </summary>
        void Execute(AttackContext context);
    }

    /// <summary>
    /// 攻击上下文 - 包含攻击所需的所有信息
    /// </summary>
    [System.Serializable]
    public struct AttackContext
    {
        public Transform Owner;
        public TeamType OwnerTeam;
        public Vector2 AimDirection;
        public Vector3 FirePosition;
        public DamageInfo DamageInfo;
        public LayerMask HitMask;
        public ProjectileConfig ProjectileConfig;
    }
}