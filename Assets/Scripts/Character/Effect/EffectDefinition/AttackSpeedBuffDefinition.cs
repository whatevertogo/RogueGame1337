using Character.Effects;
using UnityEngine;
using Character;

/// <summary>
/// 攻击速度增益效果定义
/// </summary>
[CreateAssetMenu(fileName = "AttackSpeed", menuName = "RogueGame/Character/Effects/Buffs/AttackSpeed")]
public class AttackSpeedBuffDefinition : StatusEffectDefinitionSO
{
    [Header("攻击速度增益设置")]
    [Tooltip("攻击速度修饰值")]
    public float speedModifierValue = 0.1f;

    [Tooltip("修饰符类型：Flat(固定加成) / PercentAdd(百分比累加) / PercentMult(百分比独立乘)")]
    public StatModType modifierType = StatModType.PercentAdd;

    public override StatusEffectInstanceBase CreateInstance(CharacterBase caster = null)
    {
        return new AttackSpeedBuffInstance(this, caster);
    }
}
