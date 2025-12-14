using Character.Components;
using UnityEngine;

namespace Character.Effects
{
    /// <summary>
    /// 状态效果基类
    /// </summary>
    public abstract class StatusEffectBase : IStatusEffect
    {
        public abstract string EffectId { get; }
        public virtual bool IsStackable => false;

        protected float duration;
        protected float remainingTime;
        protected CharacterStats stats;
        protected StatusEffectComponent component;

        public bool IsExpired => remainingTime <= 0;

        public StatusEffectBase(float duration)
        {
            this.duration = duration;
            this.remainingTime = duration;
        }

        public virtual void OnApply(CharacterStats stats, StatusEffectComponent comp)
        {
            this.stats = stats;
            this.component = comp;
        }

        public virtual void OnTick(float deltaTime)
        {
            remainingTime -= deltaTime;
        }

        public virtual void OnRemove(CharacterStats stats, StatusEffectComponent comp)
        {
            this.stats = null;
            this.component = null;
        }

        public virtual float ModifyOutgoingDamage(float damage) => damage;
        public virtual float ModifyIncomingDamage(float damage) => damage;

        /// <summary>
        /// 刷新持续时间
        /// </summary>
        public void Refresh()
        {
            remainingTime = duration;
        }

        /// <summary>
        /// 延长持续时间
        /// </summary>
        public void Extend(float extraTime)
        {
            remainingTime += extraTime;
        }

    }
}