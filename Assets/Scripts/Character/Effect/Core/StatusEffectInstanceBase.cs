using Character.Components;
using Character.Effects;

public abstract class StatusEffectInstanceBase : IStatusEffect
{
    public abstract string EffectId { get; }

    protected readonly bool isStackable;
    public virtual bool IsStackable => isStackable;

    protected readonly float duration;          // 0 = 无限
    protected float remainingTime;

    protected CharacterStats stats;
    protected StatusEffectComponent component;

    public bool IsExpired => duration > 0f && remainingTime <= 0f;

    protected StatusEffectInstanceBase(float duration, bool isStackable = false)
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
        if (duration > 0f)
        {
            remainingTime -= deltaTime;
        }
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
        if (duration > 0f)
            remainingTime = duration;
    }

    public void Extend(float extraTime)
    {
        if (duration > 0f)
            remainingTime += extraTime;
    }
}
