using Character.Components;

    /// <summary>
    /// 瞬时伤害效果实例
    /// 在 OnApply 时立即造成伤害，然后自动过期
    /// </summary>
    public class InstantDamageEffectInstance : StatusEffectInstanceBase
    {
        public override string EffectId => _def.effectId;

        private readonly InstantDamageEffectDefinition _def;
        private bool _damageApplied;

        public InstantDamageEffectInstance(InstantDamageEffectDefinition def)
            : base(duration: 0f, isStackable: true) // duration=0 表示瞬时效果
        {
            _def = def;
            _damageApplied = false;
        }

        public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
        {
            base.OnApply(stats, comp);

            if (_damageApplied) return;
            _damageApplied = true;

            // 计算伤害
            float damage = _def.baseDamage;
            if (_def.useAttackPower && stats != null)
            {
                damage += stats.AttackPower.Value * _def.attackPowerMultiplier;
            }

            // 应用伤害
            if (stats != null)
            {
                var damageInfo = DamageInfo.Create(damage);
                stats.TakeDamage(damageInfo);
            }

            // 标记为已过期，下一帧会被自动移除
            remainingTime = -1f;
        }
    }

