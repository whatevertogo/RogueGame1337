using Character.Components;
using Character.Effects;
using Character;

/// <summary>
/// 护甲增益效果实例
/// </summary>
public class ArmorBuffInstance : StatusEffectInstanceBase
{
    public override string EffectId => _def.effectId;

    private readonly ArmorBuffDefinition _def;
    private StatModifier _modifier;

    public ArmorBuffInstance(ArmorBuffDefinition def, CharacterBase caster = null)
        : base(def.duration, def.isStackable)
    {
        _def = def;
        _modifier = new StatModifier(_def.armorModifierValue, _def.modifierType, this);
    }

    public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
    {
        base.OnApply(stats, comp);
        stats.Armor.AddModifier(_modifier);
    }

    public override void OnRemove(CharacterStats stats, StatusEffectComponent comp)
    {
        stats.Armor.RemoveModifier(_modifier);
        base.OnRemove(stats, comp);
    }
}
