using Character.Player.Skill.Modifiers;
using Character.Player.Skill.Runtime;
using Character.Player.Skill.Targeting;
using UnityEngine;

/// <summary>
/// 能量消耗修改器：修改技能的能量消耗
/// 实现 IEnergyCostModifier 接口，在能量消耗前应用
/// </summary>
[ManagedData("Skill")]
[CreateAssetMenu(fileName = "CostSkillModifier", menuName = "RogueGame/Skill/Modifiers/CostSkillModifier")]
public class CostSkillModifier : SkillModifierBase, IEnergyCostModifier
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

    // ========== 工厂方法（使用 ScriptableObject.CreateInstance） ==========
    /// <summary>
    /// 创建能量折扣修改器
    /// </summary>
    public static CostSkillModifier Discount(float discountPercent)
    {
        var inst = CreateInstance<CostSkillModifier>();
        inst.CostMultiplier = 1f - Mathf.Clamp01(discountPercent);
        inst.CostFlat = 0;
        return inst;
    }

    /// <summary>
    /// 创建能量返还修改器（击杀后返还能量）
    /// </summary>
    public static CostSkillModifier Refund(int refundAmount)
    {
        var inst = CreateInstance<CostSkillModifier>();
        inst.CostMultiplier = 1f;
        inst.CostFlat = -Mathf.Abs(refundAmount);
        return inst;
    }

    /// <summary>
    /// 创建倍率修改器
    /// </summary>
    public static CostSkillModifier Multiplier(float multiplier)
    {
        var inst = CreateInstance<CostSkillModifier>();
        inst.CostMultiplier = Mathf.Clamp(multiplier, 0f, 10f);
        inst.CostFlat = 0;
        return inst;
    }

    /// <summary>
    /// 创建固定加值修改器
    /// </summary>
    public static CostSkillModifier Flat(int flat)
    {
        var inst = CreateInstance<CostSkillModifier>();
        inst.CostMultiplier = 1f;
        inst.CostFlat = flat;
        return inst;
    }

    // ========== IEnergyCostModifier 实现 ==========

    public void ApplyEnergyCost(ActiveSkillRuntime runtime, ref EnergyCostConfig config)
    {
        config.Multiplier *= CostMultiplier;
        config.Flat += CostFlat;
    }
}
