using Character;

public interface IEffectFactory
{
    StatusEffectInstanceBase CreateInstance(StatusEffectDefinitionSO definition, CharacterBase caster = null);
}
