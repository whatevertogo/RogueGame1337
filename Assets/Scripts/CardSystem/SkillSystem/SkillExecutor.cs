using UnityEngine;

namespace CardSystem.SkillSystem
{
    /// <summary>
    /// 技能执行器基类（ScriptableObject）
    /// 子类实现不同的执行策略（AOE / Buff / Projectile 等）
    /// </summary>
    public class SkillExecutorSO : ScriptableObject, ISkillExecutor
    {
        public virtual void Execute(SkillDefinition skill, SkillContext ctx)
        {
            // 默认实现为空，子类需重写此方法以实现具体逻辑
        }
    }
}
