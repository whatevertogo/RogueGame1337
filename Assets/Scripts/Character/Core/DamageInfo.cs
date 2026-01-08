using UnityEngine;

/// <summary>
/// 伤害信息
/// </summary>
public struct DamageInfo
{
    public float Amount;
    /// <summary>
    /// 是否为真实伤害（无视护甲）
    /// </summary>
    public bool IsTrueDamage;
    // public bool IsCrit;
    public GameObject Source;
    public Vector2 HitPoint;
    public Vector2 KnockbackDir;
    public float KnockbackForce;

    public static DamageInfo Create(float amount, bool isTrueDamage = false)
    {
        return new DamageInfo
        {
            Amount = amount,
            IsTrueDamage = isTrueDamage,
            // IsCrit = false,
            Source = null,
            KnockbackForce = 0
        };
    }
}
