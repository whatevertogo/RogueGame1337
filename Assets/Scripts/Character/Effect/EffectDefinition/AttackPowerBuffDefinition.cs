using Character.Effects;
using UnityEngine;
using Character;

/// <summary>
/// 攻击力增益效果定义
/// </summary>
[CreateAssetMenu(fileName = "AttackPowerBuffDefinition", menuName = "Character/Effects/Attack Power Buff")]
public class AttackPowerBuffDefinition : StatusEffectDefinitionSO
{
    [Header("攻击力增益设置")]
    [Tooltip("攻击力修饰值")]
    public float powerModifierValue = 10f;

    [Tooltip("修饰符类型")]
    public StatModType modifierType = StatModType.Flat;

    public override StatusEffectInstanceBase CreateInstance(CharacterBase caster = null)
    {
        return new AttackPowerBuffInstance(this, caster);
    }
}
