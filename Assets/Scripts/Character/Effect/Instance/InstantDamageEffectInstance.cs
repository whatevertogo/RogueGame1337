using Character;
using Character.Components;

    /// <summary>
    /// 瞬时伤害效果实例
    /// 在 OnApply 时立即造成伤害，然后自动过期
    /// </summary>
    public class InstantDamageEffectInstance : StatusEffectInstanceBase, IDamageSourceAware
    {
        public override string EffectId => _def.effectId;

        private readonly InstantDamageEffectDefinition _def;
        private bool _damageApplied;
        private CharacterBase _damageSource; // 新增：伤害来源

        public InstantDamageEffectInstance(InstantDamageEffectDefinition def)
            : base(duration: 0f, isStackable: true) // duration=0 表示瞬时效果
        {
            _def = def;
            _damageApplied = false;
        }

        /// <summary>
        /// 设置伤害来源
        /// </summary>
        public void SetDamageSource(CharacterBase source)
        {
            _damageSource = source;
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
                // 设置伤害来源
                if (_damageSource != null)
                {
                    damageInfo.Source = _damageSource.gameObject;
                }
                stats.TakeDamage(damageInfo);
            }

            // 标记为已过期，下一帧会被自动移除
            remainingTime = -1f;
        }
    }

