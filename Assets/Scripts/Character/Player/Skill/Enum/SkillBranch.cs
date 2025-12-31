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
    [CreateAssetMenu(fileName = "NewSkillBranch", menuName = "Skill/Definition/Branch")]
    public class SkillBranch : ScriptableObject
    {
        public string branchName;
        [TextArea] public string description;

        [Header("修改器")]
        public List<ISkillModifier> modifiers;

        [Header("该分支特有效果")]
        public List<StatusEffectComponent> effects;
    }
}