using Character.Effects;
using UnityEngine;


    /// <summary>
    /// 眩晕效果定义（控制型效果，不基于 StatModifier）
    /// </summary>
    [CreateAssetMenu(fileName = "StunEffectDefinition", menuName = "Character/Effects/Stun Effect")]
    public class StunEffectDefinition : StatusEffectDefinitionSO
    {
        public override StatusEffectInstanceBase CreateInstance()
        {
            return new StunEffectInstance(this);
        }
    }

