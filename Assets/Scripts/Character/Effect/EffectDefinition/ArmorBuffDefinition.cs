using Character.Effects;
using UnityEngine;
using Character;

/// <summary>
/// 护甲增益效果定义
/// </summary>
[CreateAssetMenu(fileName = "Armor", menuName = "RogueGame/Character/Effects/Buffs/Armor")]
public class ArmorBuffDefinition : StatusEffectDefinitionSO
{
    [Header("护甲增益设置")]
    [Tooltip("护甲修饰值")]
    public float armorModifierValue = 5f;

    [Tooltip("修饰符类型：Flat(固定加成) / PercentAdd(百分比累加) / PercentMult(百分比独立乘)")]
    public StatModType modifierType = StatModType.Flat;

    public override StatusEffectInstanceBase CreateInstance(CharacterBase caster = null)
    {
        return new ArmorBuffInstance(this, caster);
    }
}
