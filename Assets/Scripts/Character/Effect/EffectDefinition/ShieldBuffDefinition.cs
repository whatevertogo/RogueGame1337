using Character;
using Character.Effects;
using UnityEngine;


    /// <summary>
    /// 护盾效果定义（通过增加护甲实现）
    /// </summary>
    [CreateAssetMenu(fileName = "ShieldBuffDefinition", menuName = "Character/Effects/Shield Buff")]
    public class ShieldBuffDefinition : StatusEffectDefinitionSO
    {
        [Header("护盾设置")]
        [Tooltip("护甲增加值")]
        public float armorBonus = 20f;
        
        [Tooltip("修饰符类型")]
        public StatModType modifierType = StatModType.Flat;

        public override StatusEffectInstanceBase CreateInstance()
        {
            return new ShieldBuffInstance(this);
        }
    }

