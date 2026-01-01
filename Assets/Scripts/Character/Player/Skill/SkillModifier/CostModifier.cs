using Character.Player.Skill.Modifiers;
using Character.Player.Skill.Runtime;
using Character.Player.Skill.Targeting;
using UnityEngine;

/// <summary>
/// 能量消耗修改器：修改技能的能量消耗
/// 实现 IEnergyCostModifier 接口，在能量消耗前应用
/// </summary>
[CreateAssetMenu(fileName = "CostModifier", menuName = "RogueGame/Skill/Modifiers/CostModifier")]
public class CostModifier : SkillModifierBase, IEnergyCostModifier
{
    // ========== 配置属性 ==========
    /// <summary>
    /// 能量消耗倍率（默认为1，表示不改变）
    /// </summary>
    public float CostMultiplier = 1f;

    /// <summary>
    /// 能量消耗固定加值（负数表示返还）
    /// </summary>
    public int CostFlat { get; private set; }

    // ========== ISkillModifier 实现 ==========
    public override string ModifierId => $"EnergyCost({CostMultiplier}x+{CostFlat})";

    // ========== 构造函数 ==========

    /// <summary>
    /// 创建能量消耗修改器
    /// </summary>
    /// <param name="costMultiplier">能量消耗倍率（0.5表示减半，2.0表示翻倍）</param>
    /// <param name="costFlat">能量消耗固定加值</param>
    public CostModifier(float costMultiplier = 1f, int costFlat = 0)
    {
        CostMultiplier = Mathf.Clamp(costMultiplier, 0f, 10f);
        CostFlat = costFlat;
    }

    // ========== 工厂方法 ==========

    /// <summary>
    /// 创建能量折扣修改器
    /// </summary>
    /// <param name="discountPercent">折扣百分比（0.2 表示缩减20%，即消耗变为原来的80%）</param>
    public static CostModifier Discount(float discountPercent)
    {
        return new CostModifier(1f - Mathf.Clamp01(discountPercent), 0);
    }

    /// <summary>
    /// 创建能量返还修改器（击杀后返还能量）
    /// </summary>
    /// <param name="refundAmount">返还的能量值</param>
    public static CostModifier Refund(int refundAmount)
    {
        return new CostModifier(1f, -Mathf.Abs(refundAmount));
    }

    /// <summary>
    /// 创建倍率修改器
    /// </summary>
    public static CostModifier Multiplier(float multiplier)
    {
        return new CostModifier(Mathf.Clamp(multiplier, 0f, 10f), 0);
    }

    /// <summary>
    /// 创建固定加值修改器
    /// </summary>
    public static CostModifier Flat(int flat)
    {
        return new CostModifier(1f, flat);
    }

    // ========== IEnergyCostModifier 实现 ==========

    public void ApplyEnergyCost(ActiveSkillRuntime runtime, ref EnergyCostConfig config)
    {
        config.Multiplier *= CostMultiplier;
        config.Flat += CostFlat;
    }
}
