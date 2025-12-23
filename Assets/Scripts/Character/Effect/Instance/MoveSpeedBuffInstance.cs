using Character.Components;
using Character;

    /// <summary>
    /// 移动速度增益效果实例（通过 StatModifier 实现）
    /// </summary>
    public class MoveSpeedBuffInstance : StatusEffectInstanceBase
    {
        public override string EffectId => _def.effectId;

        private readonly MoveSpeedBuffDefinition _def;
        private StatModifier _modifier;

        public MoveSpeedBuffInstance(MoveSpeedBuffDefinition def, CharacterBase caster = null)
            : base(def.duration, def.isStackable)
        {
            _def = def;
            // 创建修饰符，source 指向自己，方便后续移除
            _modifier = new StatModifier(_def.speedModifierValue, _def.modifierType, this);
        }

        public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
        {
            base.OnApply(stats, comp);
            // 添加修饰符到移动速度属性
            stats.MoveSpeed.AddModifier(_modifier);
        }

        public override void OnRemove(CharacterStats stats, StatusEffectComponent comp)
        {
            // 移除修饰符
            stats.MoveSpeed.RemoveModifier(_modifier);
            base.OnRemove(stats, comp);
        }
    }

