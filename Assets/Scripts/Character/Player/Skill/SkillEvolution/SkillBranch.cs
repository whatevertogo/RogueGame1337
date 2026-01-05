using System.Collections.Generic;
using Character.Player.Skill.Modifiers;
using UnityEngine;

namespace Character.Player.Skill.Evolution
{
    /// <summary>
    /// 技能进化分支
    /// 重构：只存储修改器，效果由 IEffectGeneratorModifier 动态生成
    /// </summary>
    [CreateAssetMenu(fileName = "SkillBranch", menuName = "RogueGame/Skill/Definition/Branch")]
    public class SkillBranch : ScriptableObject
    {
        public string branchName;
        [TextArea] public string description;

        [Header("标签")]
        [Tooltip("用于UI显示的标签，帮助玩家理解分支特性")]
        public List<SkillTag> tags;

        [Tooltip("分支图标")]
        public Sprite icon;

        [Header("修改器（包含效果生成修改器）")]
        [Tooltip("所有修改器，包括参数修改器和效果生成修改器（IEffectGeneratorModifier）")]
        public List<SkillModifierBase> modifiers;
    }
}