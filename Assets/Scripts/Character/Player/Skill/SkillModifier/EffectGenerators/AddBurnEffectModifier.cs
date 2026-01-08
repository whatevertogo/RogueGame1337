using System.Collections.Generic;
using Character.Effects;
using Character.Player.Skill.Modifiers;
using Character.Player.Skill.Runtime;
using UnityEngine;

namespace Character.Player.Skill.SkillModifier.EffectGenerators
{
    /// <summary>
    /// 效果生成修改器：添加燃烧效果
    /// 示例：技能进化后附加燃烧效果
    /// </summary>
    [CreateAssetMenu(fileName = "AddBurnEffect", menuName = "RogueGame/Skill/Modifier/EffectGenerator/AddBurn")]
    public class AddBurnEffectModifier : SkillModifierBase, IEffectGeneratorModifier
    {
        [Header("燃烧效果配置")]
        [Tooltip("要添加的燃烧效果定义")]
        public StatusEffectDefinitionSO burnEffectDefinition;

        public override string ModifierId => "AddBurnEffect";

        public int Priority => 50;

        public List<StatusEffectDefinitionSO> GenerateEffects(ActiveSkillRuntime runtime)
        {
            var effects = new List<StatusEffectDefinitionSO>();

            if (burnEffectDefinition != null)
            {
                effects.Add(burnEffectDefinition);
            }

            return effects;
        }
    }
}
