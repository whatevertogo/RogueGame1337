using Character.Player.Skill.Modifiers;
using Character.Player.Skill.Runtime;
using Character.Player.Skill.Targeting;

/// <summary>
/// 伤害修正器
/// </summary>
/// <summary>
/// 伤害技能修饰器类，用于修改技能的伤害值
/// 实现了 ISkillModifier 接口
/// </summary>
public class DamageSkillModifier : ISkillModifier
{
    // 伤害倍率属性，用于对基础伤害进行乘法运算
    public float DamageMultiplier { get; private set; }
    // 伤害加值属性，用于在伤害计算后添加固定值
    public float DamageAddend { get; private set; }
    // 伤害覆盖值，当大于等于0时，将直接使用此值作为最终伤害（默认为-1，表示不覆盖）
    public float DamageOverride { get; private set; } = -1f;
    /// <summary>
    /// 是否设置为真实伤害（无视护甲）
    /// </summary>
    public bool SetTrueDamage { get; private set; }

    // 获取修饰器优先级，返回伤害倍率修饰器的优先级
    public int Priority => ModifierPriority.DAMAGE_MULTIPLIER;

    /// <summary>
    /// 创建伤害修改器
    /// </summary>
    /// <param name="damageMultiplier">伤害倍率</param>
    /// <param name="damageAddend">伤害加值</param>
    /// <param name="setTrueDamage">是否设置为真伤（默认 false）</param>
    /// <param name="source">修改器来源</param>
    public DamageSkillModifier(float damageMultiplier, float damageAddend = 0f, bool setTrueDamage = false)
    {
        DamageMultiplier = damageMultiplier;
        DamageAddend = damageAddend;
        SetTrueDamage = setTrueDamage;
    }

    /// <summary>
    /// 创建覆盖式伤害修改器（强制设置伤害值，且为真伤）
    /// </summary>
    public static DamageSkillModifier Override(float damageOverride, object source = null)
    {
        return new DamageSkillModifier(1f, 0f, true)
        {
            DamageOverride = damageOverride
        };
    }

    /// <summary>
    /// 应用伤害修饰效果
    /// </summary>
    /// <param name="runtime">技能运行时数据</param>
    /// <param name="ctx">技能目标上下文</param>
    public void Apply(ActiveSkillRuntime runtime, ref SkillTargetContext ctx)
    {
        if (runtime == null) return;

        // 计算伤害
        if (DamageOverride >= 0f)
        {
            // 如果设置了伤害覆盖值，则直接使用覆盖值作为最终伤害
            ctx.DamageResult.FinalDamage = DamageOverride;
        }
        else
        {
            // 否则使用伤害倍率和加值计算最终伤害
            var baseDamage = ctx.DamageResult.FinalDamage;
            ctx.DamageResult.FinalDamage = baseDamage * DamageMultiplier + DamageAddend;
        }

        // 设置真伤标记
        // 如果明确设置了真伤或使用了伤害覆盖值，则标记为真伤
        if (SetTrueDamage || DamageOverride >= 0f)
        {
            ctx.DamageResult.IsTrueDamage = true;
        }
    }
}