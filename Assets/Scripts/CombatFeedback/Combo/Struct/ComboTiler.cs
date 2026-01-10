using System;
using UnityEngine;

[Serializable]
public struct ComboTier
{
    [Tooltip("档位名称（普通、狂热、屠戮、毁灭等）")]
    public ComboState comboState;

    [Tooltip("触发此档位所需的最少连击数")]
    public int threshold;

    [Tooltip("连击持续时间增加（秒）")]
    public float energyMult;

    [Tooltip("移动速度加成")]
    public float speedBonus;

    [Tooltip("攻击速度加成")]
    public float attackSpeedBonus;

    [Tooltip("此档位的 UI 和特效显示颜色")]
    public Color tierColor;

    // 重载 == 运算符
    public static bool operator ==(ComboTier tier1, ComboTier tier2)
    {
        // 处理两者都是 null 的情况
        if (ReferenceEquals(tier1, null) && ReferenceEquals(tier2, null))
            return true;
        // 处理其中一个为 null 的情况
        if (ReferenceEquals(tier1, null) || ReferenceEquals(tier2, null))
            return false;

        return tier1.comboState == tier2.comboState
            && tier1.threshold == tier2.threshold
            && tier1.energyMult == tier2.energyMult
            && tier1.speedBonus == tier2.speedBonus
            && tier1.tierColor.Equals(tier2.tierColor); // 注意 Color 可能需要使用 Equals 方法比较
    }

    // 重载 != 运算符
    public static bool operator !=(ComboTier tier1, ComboTier tier2)
    {
        return !(tier1 == tier2); // 如果 == 为 true，!= 就为 false，反之亦然
    }

    // 重载 Equals 方法
    public override bool Equals(object obj)
    {
        if (obj is ComboTier tier)
        {
            return this == tier; // 使用重载的 == 运算符进行比较
        }
        return false;
    }

    // 重载 GetHashCode 方法
    public override int GetHashCode()
    {
        // 通过元组生成哈希值，保证包含所有字段
        return (comboState, threshold, energyMult, speedBonus, attackSpeedBonus, tierColor).GetHashCode();
    }
}
