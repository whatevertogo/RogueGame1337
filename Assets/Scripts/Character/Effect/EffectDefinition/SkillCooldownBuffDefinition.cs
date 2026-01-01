using Character.Effects;
using UnityEngine;
using Character;

/// <summary>
/// 技能冷却缩减效果定义
/// </summary>
[CreateAssetMenu(fileName = "SkillCooldown", menuName = "RogueGame/Character/Effects/Buffs/SkillCooldown")]
public class SkillCooldownBuffDefinition : StatusEffectDefinitionSO
{
    [Header("技能冷却缩减设置")]
    [Tooltip("冷却缩减比例(0.1 = 10%)")]
    [Range(0f, 1f)]
    public float cooldownReduction = 0.1f;

    [Tooltip("修饰符类型：建议使用 PercentAdd")]
    public StatModType modifierType = StatModType.PercentAdd;

    public override StatusEffectInstanceBase CreateInstance(CharacterBase caster = null)
    {
        return new SkillCooldownBuffInstance(this, caster);
    }
}
