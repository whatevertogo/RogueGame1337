using Character.Player.Skill.Modifiers;
using Character.Player.Skill.Runtime;
using Character.Player.Skill.Targeting;
using UnityEngine;

/// <summary>
/// 目标获取修改器：修改技能的目标获取范围、数量等参数
/// 实现 ITargetingModifier 接口，在目标获取前应用
/// </summary>
[CreateAssetMenu(fileName = "TargetingModifier", menuName = "RogueGame/Skill/Modifiers/TargetingModifier")]
public class TargetingModifier : SkillModifierBase, ITargetingModifier
{
    // ========== 配置属性 ==========
    /// <summary>
    /// 范围倍率（默认为1，表示不改变）
    /// </summary>
    public float RangeMultiplier { get; private set; } = 1f;

    /// <summary>
    /// 范围固定加值
    /// </summary>
    public float RangeAddend { get; private set; }

    /// <summary>
    /// 目标数量上限加值
    /// </summary>
    public int MaxCountAddend { get; private set; }

    /// <summary>
    /// 范围半径倍率（用于范围技能）
    /// </summary>
    public float RadiusMultiplier { get; private set; } = 1f;

    /// <summary>
    /// 范围半径加值
    /// </summary>
    public float RadiusAddend { get; private set; }

    // ========== ISkillModifier 实现 ==========
    public override string ModifierId => $"Targeting(Range×{RangeMultiplier}+{RangeAddend},Max+{MaxCountAddend})";

    // ========== 工厂方法 ==========

    /// <summary>
    /// 创建范围倍率修改器
    /// </summary>
    public static TargetingModifier RangeMul(float multiplier)
    {
        return new TargetingModifier { RangeMultiplier = multiplier };
    }

    /// <summary>
    /// 创建范围加值修改器
    /// </summary>
    public static TargetingModifier RangeAdd(float addend)
    {
        return new TargetingModifier { RangeAddend = addend };
    }

    /// <summary>
    /// 创建目标数量修改器
    /// </summary>
    public static TargetingModifier MaxCount(int addCount)
    {
        return new TargetingModifier { MaxCountAddend = addCount };
    }

    /// <summary>
    /// 创建范围半径修改器
    /// </summary>
    public static TargetingModifier Radius(float multiplier, float addend = 0f)
    {
        return new TargetingModifier { RadiusMultiplier = multiplier, RadiusAddend = addend };
    }

    /// <summary>
    /// 创建综合修改器（同时修改多个参数）
    /// </summary>
    public static TargetingModifier Create(float rangeMul = 1f, float rangeAdd = 0f, int maxCountAdd = 0, float radiusMul = 1f, float radiusAdd = 0f)
    {
        return new TargetingModifier
        {
            RangeMultiplier = rangeMul,
            RangeAddend = rangeAdd,
            MaxCountAddend = maxCountAdd,
            RadiusMultiplier = radiusMul,
            RadiusAddend = radiusAdd
        };
    }

    // ========== ITargetingModifier 实现 ==========

    public void ApplyTargeting(ActiveSkillRuntime runtime, ref TargetingConfig config)
    {
        // 应用范围修改
        config.Range = config.Range * RangeMultiplier + RangeAddend;

        // 应用目标数量修改（确保至少为1）
        config.MaxCount = Mathf.Max(1, config.MaxCount + MaxCountAddend);

        // 应用范围半径修改
        config.Radius = config.Radius * RadiusMultiplier + RadiusAddend;
    }
}
