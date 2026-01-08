using Character;
using Character.Effects;
using UnityEngine;


    /// <summary>
    /// 眩晕效果定义（控制型效果，不基于 StatModifier）
    /// </summary>
    [CreateAssetMenu(fileName = "Stun", menuName = "RogueGame/Character/Effects/Debuffs/Stun")]
    public class StunEffectDefinition : StatusEffectDefinitionSO
    {
        public override StatusEffectInstanceBase CreateInstance(CharacterBase caster = null)
        {
            return new StunEffectInstance(this, caster);
        }
    }

