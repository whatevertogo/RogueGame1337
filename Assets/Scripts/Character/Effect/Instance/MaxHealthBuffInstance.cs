using Character.Components;
using Character.Effects;
using Character;

/// <summary>
/// 生命上限增益效果实例
/// </summary>
public class MaxHealthBuffInstance : StatusEffectInstanceBase
{
    public override string EffectId => _def.effectId;

    private readonly MaxHealthBuffDefinition _def;
    private StatModifier _modifier;
    private CharacterStats _stats;
    private float _healthPercentBeforeApply; // 记录应用前的生命百分比

    public MaxHealthBuffInstance(MaxHealthBuffDefinition def, CharacterBase caster = null)
        : base(def.duration, def.isStackable)
    {
        _def = def;
        _modifier = new StatModifier(_def.healthModifierValue, _def.modifierType, this);
    }

    public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
    {
        base.OnApply(stats, comp);
        _stats = stats;

        // 记录当前生命百分比
        _healthPercentBeforeApply = stats.HPPercent;

        // 添加生命上限修饰符
        stats.MaxHP.AddModifier(_modifier);

        // 按百分比调整当前生命值，避免生命上限变化导致玩家突然死亡或满血
        stats.CurrentHP = stats.MaxHP.Value * _healthPercentBeforeApply;
    }

    public override void OnRemove(CharacterStats stats, StatusEffectComponent comp)
    {
        // 移除修饰符前记录生命百分比
        float healthPercentBeforeRemove = stats.HPPercent;

        stats.MaxHP.RemoveModifier(_modifier);

        // 按百分比调整当前生命值
        stats.CurrentHP = stats.MaxHP.Value * healthPercentBeforeRemove;

        base.OnRemove(stats, comp);
    }
}
