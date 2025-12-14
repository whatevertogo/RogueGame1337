using Character.Components;
using Character.Core;
using Character;

namespace Character.Effects
{
    /// <summary>
    /// 减速效果：降低移动速度
    /// </summary>
    public class SlowEffect : StatusEffectBase
    {
        public override string EffectId => "slow";

        private readonly float slowPercent;
        private StatModifier modifier;

        public SlowEffect(float duration, float slowPercent) : base(duration)
        {
            this.slowPercent = slowPercent;
        }

        public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
        {
            base.OnApply(stats, comp);

            // 使用修饰符系统，而不是直接修改 multiplier
            modifier = new StatModifier(-slowPercent, StatModType.PercentAdd, this);
            stats.MoveSpeed.AddModifier(modifier);
        }

        public override void OnRemove(CharacterStats stats, StatusEffectComponent comp)
        {
            // 安全移除，不会影响其他效果
            stats.MoveSpeed.RemoveModifier(modifier);
            modifier = null;

            base.OnRemove(stats, comp);
        }
    }
}