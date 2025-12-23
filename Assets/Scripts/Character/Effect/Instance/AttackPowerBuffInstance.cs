using Character.Components;
using Character.Effects;
using Character;

/// <summary>
/// 攻击力增益效果实例
/// </summary>
public class AttackPowerBuffInstance : StatusEffectInstanceBase
    {
        public override string EffectId => _def.effectId;

        private readonly AttackPowerBuffDefinition _def;
        private StatModifier _modifier;

        public AttackPowerBuffInstance(AttackPowerBuffDefinition def, CharacterBase caster = null)
            : base(def.duration, def.isStackable)
        {
            _def = def;
            _modifier = new StatModifier(_def.powerModifierValue, _def.modifierType, this);
        }

        public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
        {
            base.OnApply(stats, comp);
            stats.AttackPower.AddModifier(_modifier);
        }

        public override void OnRemove(CharacterStats stats, StatusEffectComponent comp)
        {
            stats.AttackPower.RemoveModifier(_modifier);
            base.OnRemove(stats, comp);
        }
    }

