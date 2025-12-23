using Character.Components;
using Character;

    /// <summary>
    /// 护盾效果实例（通过临时增加护甲实现）
    /// </summary>
    public class ShieldBuffInstance : StatusEffectInstanceBase
    {
        public override string EffectId => _def.effectId;

        private readonly ShieldBuffDefinition _def;
        private StatModifier _modifier;

        public ShieldBuffInstance(ShieldBuffDefinition def, CharacterBase caster = null)
            : base(def.duration, def.isStackable)
        {
            _def = def;
            _modifier = new StatModifier(_def.armorBonus, _def.modifierType, this);
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

