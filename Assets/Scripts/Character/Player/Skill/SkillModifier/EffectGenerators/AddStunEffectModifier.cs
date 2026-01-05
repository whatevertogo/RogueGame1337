using System.Collections.Generic;
using Character.Effects;
using Character.Player.Skill.Modifiers;
using Character.Player.Skill.Runtime;
using UnityEngine;

namespace Character.Player.Skill.SkillModifier.EffectGenerators
{
    /// <summary>
    /// 效果生成修改器：添加眩晕效果
    /// 示例：技能进化后附加眩晕效果
    /// </summary>
    [CreateAssetMenu(fileName = "AddStunEffect", menuName = "RogueGame/Skill/Modifier/EffectGenerator/AddStun")]
    public class AddStunEffectModifier : SkillModifierBase, IEffectGeneratorModifier
    {
        [Header("眩晕效果配置")]
        [Tooltip("要添加的眩晕效果定义")]
        public StatusEffectDefinitionSO stunEffectDefinition;

        public override string ModifierId => "AddStunEffect";

        public int Priority => 50;

        public List<StatusEffectDefinitionSO> GenerateEffects(ActiveSkillRuntime runtime)
        {
            var effects = new List<StatusEffectDefinitionSO>();

            if (stunEffectDefinition != null)
            {
                effects.Add(stunEffectDefinition);
            }

            return effects;
        }
    }
}
