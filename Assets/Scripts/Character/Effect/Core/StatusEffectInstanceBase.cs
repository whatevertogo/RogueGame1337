using Character.Components;
using UnityEngine;

namespace Character.Effects
{
    /// <summary>
    /// 状态效果基类
    /// </summary>
    public abstract class StatusEffectInstanceBase : IStatusEffect
    {
        public abstract string EffectId { get; }

        // 由构造器设置，表示此运行时实例是否可叠加
        protected readonly bool isStackable;
        public virtual bool IsStackable => isStackable;

        protected float duration;
        protected float remainingTime;
        protected CharacterStats stats;
        protected StatusEffectComponent component;

        public bool IsExpired => remainingTime <= 0;

        public StatusEffectInstanceBase(float duration, bool isStackable = false)
        {
            this.duration = duration;
            this.remainingTime = duration;
            this.isStackable = isStackable;
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

        public void Refresh()
        {
            remainingTime = duration;
        }
        public void Extend(float extraTime)
        {
            remainingTime += extraTime;
        }

    }
}