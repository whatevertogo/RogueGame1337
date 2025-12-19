using Character.Effects;
using UnityEngine;

namespace Character.Effects
{
    /// <summary>
    /// 瞬时伤害效果定义
    /// 用于技能造成的即时伤害（非持续伤害）
    /// </summary>
    [CreateAssetMenu(fileName = "InstantDamageEffectDefinition", menuName = "Character/Effects/Instant Damage")]
    public class InstantDamageEffectDefinition : StatusEffectDefinitionSO
    {
        [Header("瞬时伤害设置")]
        [Tooltip("基础伤害值")]
        public float baseDamage = 10f;
        
        [Tooltip("是否基于施法者攻击力")]
        public bool useAttackPower = true;
        
        [Tooltip("攻击力倍率")]
        public float attackPowerMultiplier = 1f;

        public override StatusEffectInstanceBase CreateInstance()
        {
            return new InstantDamageEffectInstance(this);
        }
    }
}
