using Character.Effects;
using UnityEngine;
using Character;

/// <summary>
/// 闪避率增益效果定义
/// </summary>
[CreateAssetMenu(fileName = "DodgeBuffDefinition", menuName = "Character/Effects/Dodge Buff")]
public class DodgeBuffDefinition : StatusEffectDefinitionSO
{
    [Header("闪避率增益设置")]
    [Tooltip("闪避率修饰值（0-1之间，例如0.04表示4%）")]
    public float dodgeModifierValue = 0.04f;

    [Tooltip("修饰符类型：Flat(固定加成) / PercentAdd(百分比累加) / PercentMult(百分比独立乘)")]
    public StatModType modifierType = StatModType.Flat;

    public override StatusEffectInstanceBase CreateInstance(CharacterBase caster = null)
    {
        return new DodgeBuffInstance(this, caster);
    }
}
