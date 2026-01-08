using Character;
using Character.Effects;
using UnityEngine;


public abstract class StatusEffectDefinitionSO : ScriptableObject
{
    public string effectId;
    // 是否可叠加
    public bool isStackable;
    public float duration;

    public abstract StatusEffectInstanceBase CreateInstance(CharacterBase caster = null);
}
