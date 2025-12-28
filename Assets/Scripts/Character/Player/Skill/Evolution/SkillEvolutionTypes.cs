using System.Collections.Generic;
using UnityEngine;

namespace Character.Player.Skill.Evolution
{
    /// <summary>
    /// 技能进化节点（阶段3完整实现）
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillNode", menuName = "Skill/Definition/Node")]
    public class SkillNode : ScriptableObject
    {
        [Header("等级")]
        public int level; // 2-5

        [Header("分支选项")]
        public SkillBranch branchA;
        public SkillBranch branchB;
    }

    /// <summary>
    /// 技能进化分支
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillBranch", menuName = "Skill/Definition/Branch")]
    public class SkillBranch : ScriptableObject
    {
        public string branchName;
        [TextArea] public string description;

        [Header("标签")]
        public SkillTag[] tags;

        [Header("修改器")]
        public List<ScriptableObject> modifiers;

        [Header("该分支特有效果")]
        public List<StatusEffectDefinitionSO> effects;
    }

    /// <summary>
    /// 技能标签枚举
    /// </summary>
    public enum SkillTag
    {
        AOE,        // 清杂
        Single,     // 单体
        Control,    // 控制
        Risk,       // 风险博弈
        Defense,    // 防御
        Mobile      // 位移
    }
}
