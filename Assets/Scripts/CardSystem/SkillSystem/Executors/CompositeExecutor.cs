using System.Collections.Generic;
using UnityEngine;

namespace CardSystem.SkillSystem
{
    /// <summary>
    /// 复合执行器 - 依次执行多个子执行器
    /// 可用于实现"造成伤害 + 应用减速 + 治疗自己"等复杂技能
    /// </summary>
    [CreateAssetMenu(fileName = "CompositeExecutor", menuName = "Card System/Executors/Composite")]
    public class CompositeExecutor : SkillExecutorSO
    {
        [Header("子执行器列表")]
        [Tooltip("按顺序执行的子执行器")]
        [InlineEditor]
        public List<SkillExecutorSO> executors = new();

        public override void Execute(SkillDefinition skill, SkillContext ctx)
        {
            if (executors == null || executors.Count == 0) return;

            foreach (var executor in executors)
            {
                if (executor != null)
                {
                    executor.Execute(skill, ctx);
                }
            }
        }
    }
}
