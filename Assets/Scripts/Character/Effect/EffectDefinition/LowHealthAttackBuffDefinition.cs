using Character.Effects;
using UnityEngine;
using Character;

/// <summary>
/// 低血量攻击力增益效果定义（绝境反击）
/// 当生命值低于阈值时，提供额外的攻击力加成
/// </summary>
[CreateAssetMenu(fileName = "LowHealthAttack", menuName = "RogueGame/Character/Effects/Buffs/LowHealthAttack")]
public class LowHealthAttackBuffDefinition : StatusEffectDefinitionSO
{
    [Header("低血量攻击增益设置")]
    [Tooltip("生命值阈值（0-1之间，例如0.3表示30%）")]
    [Range(0f, 1f)]
    public float hpThreshold = 0.3f;

    [Tooltip("攻击力修饰值（当HP低于阈值时生效）")]
    public float attackModifierValue = 0.25f;

    [Tooltip("修饰符类型：Flat(固定加成) / PercentAdd(百分比累加) / PercentMult(百分比独立乘)")]
    public StatModType modifierType = StatModType.PercentAdd;

    public override StatusEffectInstanceBase CreateInstance(CharacterBase caster = null)
    {
        return new LowHealthAttackBuffInstance(this, caster);
    }
}
