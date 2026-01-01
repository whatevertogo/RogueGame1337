namespace Character.Player.Skill.Pipeline
{
    using Character.Player.Skill.Core;
    using Character.Player.Skill.Targeting;

    /// <summary>
    /// Phase 接口：必须是"无状态"的
    /// ✅ 所有依赖通过 SkillContext 传递
    /// 说明：SkillContext 为可变上下文，Phase 需要能够修改上下文并使修改在后续阶段可见，
    /// 因此采用 ref 传递 SkillContext（避免不必要的拷贝并保证语义清晰）。
    /// </summary>
    public interface ISkillPhase
    {
        /// <summary>
        /// Phase 名称（用于调试和日志）
        /// </summary>
        string PhaseName { get; }

        /// <summary>
        /// 执行 Phase 逻辑（注意：ctx 使用 ref 传递，Phase 可修改 ctx）
        /// </summary>
        /// <param name="ctx">技能上下文（ref）</param>
        /// <param name="token">执行令牌</param>
        /// <returns>执行结果</returns>
        SkillPhaseResult Execute(ref SkillContext ctx, SkillExecutionToken token);
    }
}
