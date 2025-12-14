

namespace Character.Core
{
    /// <summary>
    /// 伤害结果
    /// </summary>
    public struct DamageResult
    {
        public int FinalDamage;
        // public bool IsCrit;
        public bool IsDodged;
        public bool IsKilled;

        public static DamageResult Dodged => new DamageResult { IsDodged = true };
    }
}