using Character.Components;
using Character.Effects;
using Character;

/// <summary>
/// 低血量攻击力增益效果实例（绝境反击）
/// 每帧检查HP百分比，动态应用/移除攻击力加成
/// </summary>
public class LowHealthAttackBuffInstance : StatusEffectInstanceBase
{
    public override string EffectId => _def.effectId;

    private readonly LowHealthAttackBuffDefinition _def;
    private StatModifier _modifier;
    private bool _isBuffActive = false;

    public LowHealthAttackBuffInstance(LowHealthAttackBuffDefinition def, CharacterBase caster = null)
        : base(def.duration, def.isStackable)
    {
        _def = def;
        _modifier = new StatModifier(_def.attackModifierValue, _def.modifierType, this);
    }

    public override void OnApply(CharacterStats stats, StatusEffectComponent comp)
    {
        base.OnApply(stats, comp);

        // 立即检查一次是否需要应用增益
        CheckAndApplyBuff(stats);
    }

    public override void OnTick(float deltaTime)
    {
        base.OnTick(deltaTime);

        // 每帧检查HP百分比，动态应用/移除增益
        CheckAndApplyBuff(stats);
    }

    /// <summary>
    /// 检查HP百分比并动态应用/移除增益
    /// </summary>
    private void CheckAndApplyBuff(CharacterStats stats)
    {
        if (stats == null) return;

        bool shouldBuff = stats.HPPercent < _def.hpThreshold;

        // 状态变化时应用或移除增益
        if (shouldBuff && !_isBuffActive)
        {
            // HP低于阈值，应用增益
            stats.AttackPower.AddModifier(_modifier);
            _isBuffActive = true;
        }
        else if (!shouldBuff && _isBuffActive)
        {
            // HP恢复到阈值以上，移除增益
            stats.AttackPower.RemoveModifier(_modifier);
            _isBuffActive = false;
        }
    }

    public override void OnRemove(CharacterStats stats, StatusEffectComponent comp)
    {
        // 移除时确保清理增益
        if (_isBuffActive && stats != null)
        {
            stats.AttackPower.RemoveModifier(_modifier);
            _isBuffActive = false;
        }

        base.OnRemove(stats, comp);
    }
}
