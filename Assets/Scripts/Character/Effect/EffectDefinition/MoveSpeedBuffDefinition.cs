using Character;
using Character.Effects;
using UnityEngine;


    /// <summary>
    /// 移动速度增益效果定义
    /// </summary>
    [CreateAssetMenu(fileName = "MoveSpeedBuffDefinition", menuName = "Character/Effects/Move Speed Buff")]
    public class MoveSpeedBuffDefinition : StatusEffectDefinitionSO
    {
        [Header("移动速度增益设置")]
        [Tooltip("移动速度修饰值")]
        public float speedModifierValue = 0.2f;
        
        [Tooltip("修饰符类型：Flat(固定加成) / PercentAdd(百分比累加) / PercentMult(百分比独立乘)")]
        public StatModType modifierType = StatModType.PercentAdd;

        public override StatusEffectInstanceBase CreateInstance()
        {
            return new MoveSpeedBuffInstance(this);
        }
    }

