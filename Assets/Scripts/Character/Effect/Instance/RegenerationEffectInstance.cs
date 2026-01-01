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
        
        // 验证配置
        if (_def.tickInterval <= 0f)
        {
            CDTU.Utils.CDLogger.LogWarning($"[RegenerationEffectInstance] tickInterval <= 0 ({_def.tickInterval}), 将使用默认值 1f");
            _def.tickInterval = 1f;
        }
    }

    public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
    {
        base.OnApply(stats, comp);
        _elapsedTime = 0f;

    }

    public override void OnTick(float deltaTime)
    {
        base.OnTick(deltaTime);

        if (IsExpired)
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

        // 使用 Heal() 方法以触发 OnHealed 和 OnHealthChanged 事件
        stats.Heal(_def.healPerTick);
    }
}
