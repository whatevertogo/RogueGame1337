using Character.Effects;
using UnityEngine;

namespace Character.Effects
{
    /// <summary>
    /// 伤害减免增益效果（通过修改受到伤害实现）
    /// </summary>
    [CreateAssetMenu(fileName = "DamageReductionBuffDefinition", menuName = "Character/Effects/Damage Reduction Buff")]
    public class DamageReductionBuffDefinition : StatusEffectDefinitionSO
    {
        [Header("伤害减免设置")]
        [Tooltip("伤害减免百分比，例如 0.3 表示减少30%受到伤害")]
        [Range(0f, 1f)]
        public float reductionPercent = 0.3f;

        public override StatusEffectInstanceBase CreateInstance()
        {
            return new DamageReductionBuffInstance(this);
        }
    }
}
