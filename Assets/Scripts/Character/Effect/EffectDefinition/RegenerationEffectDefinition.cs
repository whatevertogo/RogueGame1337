using Character.Effects;
using UnityEngine;
using Character;

/// <summary>
/// 生命再生效果定义（按间隔回复生命）
/// </summary>
[CreateAssetMenu(
    fileName = "RegenerationEffectDefinition",
    menuName = "Character/Effects/Regeneration")]
public class RegenerationEffectDefinition : StatusEffectDefinitionSO
{
    [Header("生命再生设置")]

    [Tooltip("每次触发回复的生命值")]
    public float healPerTick = 2f;

    [Tooltip("再生触发间隔（秒）")]
    public float tickInterval = 1f;

    public override StatusEffectInstanceBase CreateInstance(CharacterBase caster = null)
    {
        return new RegenerationEffectInstance(this, caster);
    }
}
