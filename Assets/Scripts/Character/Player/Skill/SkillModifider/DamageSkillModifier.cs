using Character.Player.Skill.Modifiers;
using Character.Player.Skill.Runtime;
using Character.Player.Skill.Targeting;

/// <summary>
/// 伤害修正器
/// </summary>
public class DamageSkillModifier : ISkillModifier
{
    public float DamageMultiplier { get; private set; }
    public float DamageAddend { get; private set; }
    public float DamageOverride { get; private set; } = -1f;
    /// <summary>
    /// 是否设置为真实伤害（无视护甲）
    /// </summary>
    public bool SetTrueDamage { get; private set; }

    public int Priority => ModifierPriority.DAMAGE_MULTIPLIER;

    /// <summary>
    /// 修改器来源标识（用于批量移除）
    /// </summary>
    public object Source { get; private set; }

    /// <summary>
    /// 创建伤害修改器
    /// </summary>
    /// <param name="damageMultiplier">伤害倍率</param>
    /// <param name="damageAddend">伤害加值</param>
    /// <param name="setTrueDamage">是否设置为真伤（默认 false）</param>
    /// <param name="source">修改器来源</param>
    public DamageModifier(float damageMultiplier, float damageAddend = 0f, bool setTrueDamage = false, object source = null)
    {
        DamageMultiplier = damageMultiplier;
        DamageAddend = damageAddend;
        SetTrueDamage = setTrueDamage;
        Source = source;
    }

    /// <summary>
    /// 创建覆盖式伤害修改器（强制设置伤害值，且为真伤）
    /// </summary>
    public static DamageModifier Override(float damageOverride, object source = null)
    {
        return new DamageModifier(1f, 0f, true, source)
        {
            DamageOverride = damageOverride
        };
    }

    public void Apply(ActiveSkillRuntime runtime, ref SkillTargetContext ctx)
    {
        if (runtime == null) return;

        // 计算伤害
        if (DamageOverride >= 0f)
        {
            ctx.DamageResult.FinalDamage = DamageOverride;
        }
        else
        {
            var baseDamage = ctx.DamageResult.FinalDamage;
            ctx.DamageResult.FinalDamage = baseDamage * DamageMultiplier + DamageAddend;
        }

        // 设置真伤标记
        if (SetTrueDamage || DamageOverride >= 0f)
        {
            ctx.DamageResult.IsTrueDamage = true;
        }
    }
}