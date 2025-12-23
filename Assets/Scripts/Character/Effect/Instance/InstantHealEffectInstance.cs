using Character.Components;
using Character;
    /// <summary>
    /// 瞬时治疗效果实例
    /// 在 OnApply 时立即治疗，然后自动过期
    /// </summary>
    public class InstantHealEffectInstance : StatusEffectInstanceBase
    {
        public override string EffectId => _def.effectId;

        private readonly InstantHealEffectDefinition _def;
        private bool _healApplied;

        public InstantHealEffectInstance(InstantHealEffectDefinition def, CharacterBase caster = null)
            : base(duration: 0f, isStackable: true) // duration=0 表示瞬时效果
        {
            _def = def;
            _healApplied = false;
        }

        public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
        {
            base.OnApply(stats, comp);

            if (_healApplied) return;
            _healApplied = true;

            // 计算治疗量
            float healAmount = _def.baseHealAmount;
            if (_def.useMaxHPPercent && stats != null)
            {
                healAmount = stats.MaxHP.Value * _def.maxHPPercent;
            }

            // 应用治疗
            if (stats != null)
            {
                stats.Heal(healAmount);
            }

            // 标记为已过期，下一帧会被自动移除
            remainingTime = -1f;
        }
    }

