using Character;
using UnityEngine;

public static class EffectFactory
{
    public static StatusEffectInstanceBase CreateInstance(StatusEffectDefinitionSO definition, CharacterBase caster = null)
    {

        var instance = definition.CreateInstance(caster);
        return instance;
    }

    public static AttackSpeedBuffDefinition CreateAttackSpeedBuffDefinition(float speedModifierValue, StatModType modifierType)
    {
        var def = ScriptableObject.CreateInstance<AttackSpeedBuffDefinition>();
        def.speedModifierValue = speedModifierValue;
        def.modifierType = modifierType;

        return def;
    }
}