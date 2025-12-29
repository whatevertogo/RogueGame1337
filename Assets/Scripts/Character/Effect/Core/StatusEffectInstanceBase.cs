using Character.Components;
using Character.Effects;

public abstract class StatusEffectInstanceBase : IStatusEffect
{
    public abstract string EffectId { get; }

    protected readonly bool isStackable;
    public virtual bool IsStackable => isStackable;

    /// <summary>
    /// 是否为真实伤害（仅对伤害类效果生效）
    /// </summary>
    public bool IsTrueDamage { get; set; }

    protected float duration;          // 0 = 无限（移除 readonly 以支持动态修改）
    protected float remainingTime;

    protected CharacterStats stats;
    protected StatusEffectComponent component;

    public bool IsExpired => duration > 0f && remainingTime <= 0f;

    protected StatusEffectInstanceBase(float duration, bool isStackable = false)
    {
        this.duration = duration;
        this.remainingTime = duration;
        this.isStackable = isStackable;
        IsTrueDamage = false;
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

    /// <summary>
    /// 设置效果的持续时间（用于动态调整持续时间，例如临时 Buff）
    /// </summary>
    /// <param name="newDuration">新的持续时间（秒），0 表示永久</param>
    public void SetDuration(float newDuration)
    {
        duration = newDuration;
        remainingTime = newDuration;
    }
}
