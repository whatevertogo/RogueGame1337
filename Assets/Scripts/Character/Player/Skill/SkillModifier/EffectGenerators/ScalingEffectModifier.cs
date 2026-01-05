using System.Collections.Generic;
using Character.Effects;
using Character.Player.Skill.Modifiers;
using Character.Player.Skill.Runtime;
using UnityEngine;

namespace Character.Player.Skill.SkillModifier.EffectGenerators
{
    /// <summary>
    /// 缩放效果生成修改器：根据技能等级动态生成不同强度的效果
    /// 示例：每级增强效果强度
    /// 注意：这是一个高级示例，需要配合支持参数缩放的 StatusEffectDefinitionSO
    /// </summary>
    [CreateAssetMenu(fileName = "ScalingEffect", menuName = "RogueGame/Skill/Modifier/EffectGenerator/Scaling")]
    public class ScalingEffectModifier : SkillModifierBase, IEffectGeneratorModifier
    {
        [Header("基础效果")]
        [Tooltip("基础效果定义（实际数值会根据等级缩放）")]
        public StatusEffectDefinitionSO baseEffectDefinition;

        [Header("缩放配置")]
        [Tooltip("每级增加的效果强度百分比（例如：0.1 = 10%）")]
        public float perLevelScaling = 0.1f;

        public override string ModifierId => "ScalingEffect";

        public int Priority => 50;

        public List<StatusEffectDefinitionSO> GenerateEffects(ActiveSkillRuntime runtime)
        {
            var effects = new List<StatusEffectDefinitionSO>();

            if (baseEffectDefinition != null)
            {
                // 注意：这里直接返回基础效果定义
                // 实际的缩放应该在效果实例化时处理（EffectFactory 或 StatusEffectInstance）
                // 或者创建一个修改版的定义（需要额外的序列化支持）
                
                // 简化实现：直接返回基础效果
                // 更复杂的实现可以：
                // 1. 在 EffectFactory 中检测是否来自 ScalingEffectModifier
                // 2. 根据 runtime.CurrentLevel 动态调整效果参数
                
                effects.Add(baseEffectDefinition);
                
                Debug.Log($"[ScalingEffectModifier] 技能等级 {runtime.CurrentLevel}，效果强度缩放: {1f + (runtime.CurrentLevel - 1) * perLevelScaling}x");
            }

            return effects;
        }
    }
}
