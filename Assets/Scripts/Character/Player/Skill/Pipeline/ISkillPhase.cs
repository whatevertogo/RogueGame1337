namespace Character.Player.Skill.Pipeline
{
    using Character.Player.Skill.Core;
    using Character.Player.Skill.Targeting;

    /// <summary>
    /// Phase 接口：必须是"无状态"的
    /// ❌ 不要在 Phase 内缓存字段
    /// ❌ 不要持有 Runtime
    /// ✅ 所有依赖通过 SkillContext 传递
    /// </summary>
    public interface ISkillPhase
    {
        /// <summary>
        /// Phase 名称（用于调试和日志）
        /// </summary>
        string PhaseName { get; }

        /// <summary>
        /// 执行 Phase 逻辑
        /// </summary>
        /// <param name="ctx">技能上下文</param>
        /// <param name="token">执行令牌</param>
        /// <returns>执行结果</returns>
        SkillPhaseResult Execute( SkillContext ctx, SkillExecutionToken token);
    }
}
