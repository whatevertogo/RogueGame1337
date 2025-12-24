using Character;
using Character.Components;

public class RegenerationEffectInstance : StatusEffectInstanceBase
{
    public override string EffectId => _def.effectId;

    private readonly RegenerationEffectDefinition _def;
    private float _elapsedTime;

    public RegenerationEffectInstance(
        RegenerationEffectDefinition def,
        CharacterBase caster = null)
        : base(def.duration, def.isStackable)
    {
        _def = def;
    }

    public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
    {
        base.OnApply(stats, comp);
        _elapsedTime = 0f;

        // tickInterval <= 0 表示立即生效一次
        if (_def.tickInterval <= 0f)
        {
            Heal();
        }
    }

    public override void OnTick(float deltaTime)
    {
        base.OnTick(deltaTime);

        if (IsExpired)
            return;

        if (_def.tickInterval <= 0f)
            return;

        _elapsedTime += deltaTime;

        while (_elapsedTime >= _def.tickInterval)
        {
            Heal();
            _elapsedTime -= _def.tickInterval;
        }
    }

    private void Heal()
    {
        if (stats == null || stats.IsDead)
            return;

        stats.CurrentHP += _def.healPerTick;
    }
}
