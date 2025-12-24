using Character.Effects;
using UnityEngine;
using Character;

/// <summary>
/// 生命上限增益效果定义
/// </summary>
[CreateAssetMenu(fileName = "MaxHealthBuffDefinition", menuName = "Character/Effects/Max Health Buff")]
public class MaxHealthBuffDefinition : StatusEffectDefinitionSO
{
    [Header("生命上限增益设置")]
    [Tooltip("生命上限修饰值")]
    public float healthModifierValue = 10f;

    [Tooltip("修饰符类型")]
    public StatModType modifierType = StatModType.Flat;

    public override StatusEffectInstanceBase CreateInstance(CharacterBase caster = null)
    {
        return new MaxHealthBuffInstance(this, caster);
    }
}
