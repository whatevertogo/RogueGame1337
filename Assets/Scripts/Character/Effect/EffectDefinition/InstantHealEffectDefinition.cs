using Character.Effects;
using UnityEngine;


    /// <summary>
    /// 瞬时治疗效果定义
    /// 用于技能造成的即时治疗（非持续治疗）
    /// </summary>
    [CreateAssetMenu(fileName = "InstantHealEffectDefinition", menuName = "Character/Effects/Instant Heal")]
    public class InstantHealEffectDefinition : StatusEffectDefinitionSO
    {
        [Header("瞬时治疗设置")]
        [Tooltip("基础治疗量")]
        public float baseHealAmount = 20f;
        
        [Tooltip("是否基于目标最大生命值百分比")]
        public bool useMaxHPPercent = false;
        
        [Tooltip("最大生命值百分比（当 useMaxHPPercent=true 时生效）")]
        [Range(0f, 1f)]
        public float maxHPPercent = 0.1f;

        public override StatusEffectInstanceBase CreateInstance()
        {
            return new InstantHealEffectInstance(this);
        }
    }

