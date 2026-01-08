
using Character;

public class EffectFactory : IEffectFactory
{
    public StatusEffectInstanceBase CreateInstance(StatusEffectDefinitionSO definition, CharacterBase caster = null)
    {

        var instance = definition.CreateInstance(caster);
        return instance;
    }
}