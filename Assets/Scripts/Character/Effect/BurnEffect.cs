using UnityEngine;
using Character.Core;
using Character.Components;

namespace Character.Effects
{
    /// <summary>
    /// 燃烧效果：每秒造成伤害
    /// </summary>
    public class BurnEffect : StatusEffectBase
    {
        public override string EffectId => "burn";
        public override bool IsStackable => true;  // 可叠加

        private readonly float tickDamage;
        private readonly float tickInterval;
        private float tickTimer;
        private HealthComponent health;

        public BurnEffect(float duration, float damagePerTick, float tickInterval = 1f)
            : base(duration)
        {
            this.tickDamage = damagePerTick;
            this.tickInterval = tickInterval;
            this.tickTimer = 0f;
        }

        public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
        {
            base.OnApply(stats, comp);
            health = comp.GetComponent<HealthComponent>();
        }

        public override void OnTick(float deltaTime)
        {
            base.OnTick(deltaTime);

            tickTimer += deltaTime;
            if (tickTimer >= tickInterval)
            {
                tickTimer -= tickInterval;

                if (health != null)
                {
                    var damageInfo = DamageInfo.Create(tickDamage);
                    health.TakeDamage(damageInfo);
                }
            }
        }
    }
}