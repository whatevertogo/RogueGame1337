

using Character.Effects;
using UnityEngine;

[CreateAssetMenu(fileName = "BurnEffectDefinition", menuName = "Character/Effects/Burn Effect")]
public class BurnEffectDefinitionSO : StatusEffectDefinitionSO
{
    public float damagePerSecond;

    public override StatusEffectInstanceBase CreateInstance()
    {
        return new BurnEffectInstance(this);
    }
}