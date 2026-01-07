using System.Collections.Generic;
using Character.Effects;
using Character.Player.Skill.Modifiers;
using Character.Player.Skill.Runtime;
using UnityEngine;

namespace Character.Player.Skill.SkillModifier.EffectGenerators
{
    /// <summary>
    /// 条件效果生成修改器：根据条件动态生成效果
    /// </summary>
    [CreateAssetMenu(fileName = "ConditionalEffect", menuName = "RogueGame/Skill/Modifier/EffectGenerator/Conditional")]
    public class ConditionalEffectModifier : SkillModifierBase, IEffectGeneratorModifier
    {
        [Header("条件配置")]
        [Tooltip("最低技能等级要求")]
        public int minLevel = 1;

        [Header("效果配置")]
        [Tooltip("条件满足时添加的效果")]
        public List<StatusEffectDefinitionSO> effectsWhenConditionMet;

        public override string ModifierId => "ConditionalEffect";

        public int Priority => 50;

        public List<StatusEffectDefinitionSO> GenerateEffects(ActiveSkillRuntime runtime)
        {
            var effects = new List<StatusEffectDefinitionSO>();

            // 检查条件：当前等级是否满足最低要求
            if (runtime.CurrentLevel >= minLevel)
            {
                if (effectsWhenConditionMet != null)
                {
                    effects.AddRange(effectsWhenConditionMet);
                }
            }

            return effects;
        }
    }
}
