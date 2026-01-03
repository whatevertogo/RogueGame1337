using System.Collections.Generic;
using Character.Components;
using Character.Effects;
using Character.Player.Skill.Modifiers;
using UnityEngine;

namespace Character.Player.Skill.Evolution
{
    /// <summary>
    /// 技能进化分支
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

        [Header("修改器")]
        public List<SkillModifierBase> modifiers;

        [Header("该分支特有效果")]
        public List<StatusEffectComponent> effects;
    }
}