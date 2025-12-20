using Character.Components;
using Character.Core;


    /// <summary>
    /// 减速减益效果实例
    /// </summary>
    public class SlowDebuffInstance : StatusEffectInstanceBase
    {
        public override string EffectId => _def.effectId;

        private readonly SlowDebuffDefinition _def;
        private StatModifier _modifier;

        public SlowDebuffInstance(SlowDebuffDefinition def)
            : base(def.duration, def.isStackable)
        {
            _def = def;
            _modifier = new StatModifier(_def.slowModifierValue, _def.modifierType, this);
        }

        public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
        {
            base.OnApply(stats, comp);
            stats.MoveSpeed.AddModifier(_modifier);
        }

        public override void OnRemove(CharacterStats stats, StatusEffectComponent comp)
        {
            stats.MoveSpeed.RemoveModifier(_modifier);
            base.OnRemove(stats, comp);
        }
    }

