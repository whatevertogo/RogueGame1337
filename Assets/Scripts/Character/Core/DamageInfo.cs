using UnityEngine;

namespace Character.Core
{
    /// <summary>
    /// 伤害信息
    /// </summary>
    public struct DamageInfo
    {
        public float Amount;
        // public bool IsCrit;
        public GameObject Source;
        public Vector2 HitPoint;
        public Vector2 KnockbackDir;
        public float KnockbackForce;

        public static DamageInfo Create(float amount)
        {
            return new DamageInfo
            {
                Amount = amount,
                // IsCrit = false,
                Source = null,
                KnockbackForce = 0
            };
        }
    }

}