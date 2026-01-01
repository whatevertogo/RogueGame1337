
using UnityEngine;

namespace Character.Player.Skill.Evolution
{/// <summary>
 /// 技能进化节点（阶段3完整实现）
 /// </summary>
    [CreateAssetMenu(fileName = "SkillNode", menuName = "RogueGame/Skill/Definition/Node")]
    public class SkillNode : ScriptableObject
    {
        [Header("等级")]
        public int level; // 2-5

        [Header("分支选项")]
        public SkillBranch branchA;
        public SkillBranch branchB;
    }
}