using Character;
using Character.Effects;
using UnityEngine;


/// <summary>
/// 减速减益效果定义
/// </summary>
[CreateAssetMenu(fileName = "Slow", menuName = "RogueGame/Character/Effects/Debuffs/Slow")]
public class SlowDebuffDefinition : StatusEffectDefinitionSO
{
    [Header("减速设置")]
    [Tooltip("减速值（负数），例如 -0.3 表示减少30%移动速度")]
    public float slowModifierValue = -0.3f;

    [Tooltip("修饰符类型")]
    public StatModType modifierType = StatModType.PercentAdd;

    public override StatusEffectInstanceBase CreateInstance(CharacterBase caster = null)
    {
        return new SlowDebuffInstance(this, caster);
    }
}
