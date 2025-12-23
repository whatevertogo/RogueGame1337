using Character.Components;
using Character;

/// <summary>
/// 眩晕效果实例（行为控制型效果）
/// 这类效果不通过 StatModifier 实现，而是直接设置状态标记
/// </summary>
public class StunEffectInstance : StatusEffectInstanceBase
{
    public override string EffectId => _def.effectId;

    private readonly StunEffectDefinition _def;

    public StunEffectInstance(StunEffectDefinition def, CharacterBase caster = null)
        : base(def.duration, def.isStackable)
    {
        _def = def;
    }

    public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
    {
        base.OnApply(stats, comp);
        // 设置眩晕状态
        comp.SetStunned(true);
    }

    public override void OnRemove(CharacterStats stats, StatusEffectComponent comp)
    {
        // 解除眩晕状态
        comp.SetStunned(false);
        base.OnRemove(stats, comp);
    }
}
