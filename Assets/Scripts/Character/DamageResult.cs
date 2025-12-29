using UnityEngine;

/// <summary>
/// 伤害计算结果（独立结构体，用于 DamageModifier）
/// </summary>
public struct DamageResult
{
    /// <summary>
    /// 伤害倍率（由修改器修改，默认 1.0）
    /// </summary>
    public float PowerMultiplier;

    /// <summary>
    /// 固定伤害加值（由修改器添加）
    /// </summary>
    public float FlatDamage;

    /// <summary>
    /// 是否为真实伤害（无视防御）
    /// </summary>
    public bool IsTrueDamage;

    /// <summary>
    /// 最终伤害值（由修改器计算后传递到效果系统）
    /// </summary>
    public float FinalDamage;

    public GameObject Source;
    /// <summary>
    /// 计算最终伤害（在应用所有修改器后调用）
    /// </summary>
    public void CalculateFinalDamage(float baseDamage)
    {
        FinalDamage = (baseDamage * PowerMultiplier) + FlatDamage;
    }

    /// <summary>
    /// 初始化默认值
    /// </summary>
    public static DamageResult Default => new DamageResult
    {
        PowerMultiplier = 1.0f,
        FlatDamage = 0f,
        IsTrueDamage = false,
        FinalDamage = 0f,
        Source = null
    };
}
