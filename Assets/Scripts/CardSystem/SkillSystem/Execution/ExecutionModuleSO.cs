using UnityEngine;

namespace CardSystem.SkillSystem.Execution
{
    /// <summary>
    /// 抽象的技能执行模块（ScriptableObject）。
    /// 该模块负责把 SkillContext 中的目标列表或显式目标变为实际效果（造成伤害/施放投射物/施加状态效果等）。
    /// 实现类应保证在目标为空或不合法时安全返回，不抛异常。
    /// </summary>
    public abstract class ExecutionModuleSO : ScriptableObject
    {
        /// <summary>
        /// 在给定的上下文上执行技能效果。实现应使用 SkillContext 提供的方法（例如 ApplyStatusEffect / ApplyDamage）来作用于目标。
        /// </summary>
        /// <param name="ctx">技能上下文，包含 Owner、Position、Targets、ExplicitTarget 等信息</param>
        public abstract void Execute(SkillContext ctx);
    }
}
