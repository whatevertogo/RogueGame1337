using Character.Effects;
using UnityEngine;
using Character;

    /// <summary>
    /// 伤害减免增益效果（通过修改受到伤害实现）
    /// </summary>
    [CreateAssetMenu(fileName = "DamageReduction", menuName = "RogueGame/Character/Effects/Buffs/DamageReduction")]
    public class DamageReductionBuffDefinition : StatusEffectDefinitionSO
    {
        [Header("伤害减免设置")]
        [Tooltip("伤害减免百分比，例如 0.3 表示减少30%受到伤害")]
        [Range(0f, 1f)]
        public float reductionPercent = 0.3f;

        public override StatusEffectInstanceBase CreateInstance(CharacterBase caster = null)
        {
            return new DamageReductionBuffInstance(this, caster);
        }
    }

