using Character.Components;

namespace Character.Effects
{
    /// <summary>
    /// 伤害减免效果实例
    /// 这是一个特殊类型的效果，不修改属性，而是通过 Hook 修改伤害计算
    /// </summary>
    public class DamageReductionBuffInstance : StatusEffectInstanceBase
    {
        public override string EffectId => _def.effectId;

        private readonly DamageReductionBuffDefinition _def;

        public DamageReductionBuffInstance(DamageReductionBuffDefinition def)
            : base(def.duration, def.isStackable)
        {
            _def = def;
        }

        /// <summary>
        /// 修改受到的伤害（减少伤害）
        /// </summary>
        public override float ModifyIncomingDamage(float damage)
        {
            return damage * (1f - _def.reductionPercent);
        }
    }
}
