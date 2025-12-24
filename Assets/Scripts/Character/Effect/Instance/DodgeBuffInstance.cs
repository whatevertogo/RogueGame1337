using Character.Components;
using Character.Effects;
using Character;

/// <summary>
/// 闪避率增益效果实例
/// </summary>
public class DodgeBuffInstance : StatusEffectInstanceBase
{
    public override string EffectId => _def.effectId;

    private readonly DodgeBuffDefinition _def;
    private StatModifier _modifier;

    public DodgeBuffInstance(DodgeBuffDefinition def, CharacterBase caster = null)
        : base(def.duration, def.isStackable)
    {
        _def = def;
        _modifier = new StatModifier(_def.dodgeModifierValue, _def.modifierType, this);
    }

    public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
    {
        base.OnApply(stats, comp);
        stats.Dodge.AddModifier(_modifier);
    }

    public override void OnRemove(CharacterStats stats, StatusEffectComponent comp)
    {
        stats.Dodge.RemoveModifier(_modifier);
        base.OnRemove(stats, comp);
    }
}
