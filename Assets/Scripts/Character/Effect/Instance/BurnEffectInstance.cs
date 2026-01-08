using Character.Components;
using Character;
    /// <summary>
    /// 灼烧效果实例（持续伤害型效果）
    /// </summary>
    public class BurnEffectInstance : StatusEffectInstanceBase, IDamageSourceAware
    {
        public override string EffectId => _def.effectId;

        private readonly BurnEffectDefinitionSO _def;
        private CharacterBase _damageSource;

        public BurnEffectInstance(BurnEffectDefinitionSO def, CharacterBase caster = null)
            : base(def.duration, def.isStackable)
        {
            _def = def;
            if (caster != null) SetDamageSource(caster);
        }

        public void SetDamageSource(CharacterBase source)
        {
            _damageSource = source;
        }

        public override void OnTick(float dt)
        {
            base.OnTick(dt);
            // 每帧造成伤害
            if (stats != null)
            {
                var damageInfo = DamageInfo.Create(_def.damagePerSecond * dt, IsTrueDamage);
                // 设置伤害来源
                if (_damageSource != null)
                {
                    damageInfo.Source = _damageSource.gameObject;
                }
                stats.TakeDamage(damageInfo);
            }
        }
    }
