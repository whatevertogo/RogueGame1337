using Character.Player.Skill.Modifiers;
using Character.Player.Skill.Runtime;
using UnityEngine;

/// <summary>
/// 伤害修改器：修改技能的最终伤害和真实伤害标记
/// 实现 IDamageModifier 接口，在伤害计算阶段应用
/// </summary>
[CreateAssetMenu(fileName = "DamageSkillModifier", menuName = "RogueGame/Skill/Modifiers/DamageSkillModifier")]
public class DamageSkillModifier : SkillModifierBase, IDamageModifier
{
    // ========== 配置属性 ==========
    /// <summary>
    /// 伤害倍率，用于对基础伤害进行乘法运算
    /// </summary>
    public float DamageMultiplier;

    /// <summary>
    /// 伤害加值，用于在伤害计算后添加固定值
    /// </summary>
    public float DamageAddend;

    /// <summary>
    /// 伤害覆盖值，当大于等于0时，将直接使用此值作为最终伤害（默认为-1，表示不覆盖）
    /// </summary>
    public float DamageOverride = -1f;

    /// <summary>
    /// 是否设置为真实伤害（无视护甲）
    /// </summary>
    public bool SetTrueDamage;

    // ========== ISkillModifier 实现 ==========
    public override string ModifierId => $"Damage({DamageMultiplier}x+{DamageAddend},Override:{DamageOverride})";

    // ========== 工厂方法 ==========

    /// <summary>
    /// 创建标准伤害修改器（倍率 + 加值）
    /// </summary>
    /// <param name="damageMultiplier">伤害倍率</param>
    /// <param name="damageAddend">伤害加值</param>
    /// <param name="setTrueDamage">是否设置为真伤（默认 false）</param>
    public DamageSkillModifier(float damageMultiplier, float damageAddend = 0f, bool setTrueDamage = false)
    {
        DamageMultiplier = damageMultiplier;
        DamageAddend = damageAddend;
        SetTrueDamage = setTrueDamage;
    }

    /// <summary>
    /// 创建覆盖式伤害修改器（强制设置伤害值，且为真伤）
    /// </summary>
    /// <param name="damageOverride">覆盖的伤害值</param>
    public static DamageSkillModifier Override(float damageOverride)
    {
        return new DamageSkillModifier(1f, 0f, true)
        {
            DamageOverride = damageOverride
        };
    }

    // ========== IDamageModifier 实现 ==========

    /// <summary>
    /// 应用伤害修改（仅修改传入的 DamageResult）
    /// </summary>
    /// <param name="runtime">技能运行时数据</param>
    /// <param name="result">伤害计算结果（ref 引用，可直接修改）</param>
    public void ApplyDamage(ActiveSkillRuntime runtime, ref DamageResult result)
    {
        if (runtime == null) return;

        // 计算伤害
        if (DamageOverride >= 0f)
        {
            // 覆盖模式：直接使用覆盖值
            result.FinalDamage = DamageOverride;
            result.IsTrueDamage = true;  // 覆盖模式默认为真伤
        }
        else
        {
            // 倍率+加值模式
            result.FinalDamage = result.FinalDamage * DamageMultiplier + DamageAddend;

            // 如果明确设置了真伤标记
            if (SetTrueDamage)
            {
                result.IsTrueDamage = true;
            }
        }
    }
}
