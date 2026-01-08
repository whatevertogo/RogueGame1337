using Character.Components;
using Character.Effects;
using Character;

/// <summary>
/// 攻击速度增益效果实例
/// </summary>
public class AttackSpeedBuffInstance : StatusEffectInstanceBase
{
    public override string EffectId => _def.effectId;

    private readonly AttackSpeedBuffDefinition _def;
    private StatModifier _modifier;

    public AttackSpeedBuffInstance(AttackSpeedBuffDefinition def, CharacterBase caster = null)
        : base(def.duration, def.isStackable)
    {
        _def = def;
        _modifier = new StatModifier(_def.speedModifierValue, _def.modifierType, this);
    }

    public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
    {
        base.OnApply(stats, comp);
        stats.AttackSpeed.AddModifier(_modifier);
    }

    public override void OnRemove(CharacterStats stats, StatusEffectComponent comp)
    {
        stats.AttackSpeed.RemoveModifier(_modifier);
        base.OnRemove(stats, comp);
    }
}
