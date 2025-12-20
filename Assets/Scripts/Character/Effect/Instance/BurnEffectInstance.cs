using Character.Components;
using Character.Core;

    /// <summary>
    /// 灼烧效果实例（持续伤害型效果）
    /// </summary>
    public class BurnEffectInstance : StatusEffectInstanceBase
    {
        public override string EffectId => _def.effectId;
        
        private readonly BurnEffectDefinitionSO _def;

        public BurnEffectInstance(BurnEffectDefinitionSO def)
            : base(def.duration, def.isStackable)
        {
            _def = def;
        }

        public override void OnTick(float dt)
        {
            base.OnTick(dt);
            // 每帧造成伤害
            if (stats != null)
            {
                stats.TakeDamage(DamageInfo.Create(_def.damagePerSecond * dt));
            }
        }
    }
