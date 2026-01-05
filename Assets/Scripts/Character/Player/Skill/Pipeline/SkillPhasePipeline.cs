using Character.Player.Skill.Core;
using Character.Player.Skill.Targeting;
using System.Collections.Generic;
namespace Character.Player.Skill.Pipeline
{

    /// <summary>
    /// 技能阶段执行管道
    /// 设计要点：
    /// 1. 同步执行 - Execute 方法完全同步
    /// 2. 链式调用 - 支持流式 API（Add 方法返回 this）
    /// 3. 不可变构建 - 一旦构建完成，Phase 列表不应修改
    /// </summary>
    public sealed class SkillPhasePipeline
    {
        private readonly List<ISkillPhase> _phases = new List<ISkillPhase>();

        /// <summary>
        /// 添加 Phase 到 Pipeline
        /// </summary>
        public SkillPhasePipeline Add(ISkillPhase phase)
        {
            if (phase != null)
            {
                _phases.Add(phase);
            }
            return this;
        }

        /// <summary>
        /// 执行 Pipeline
        /// </summary>
        /// <param name="ctx">技能上下文（ref，可被 Phase 修改）</param>
        /// <param name="token">执行令牌</param>
        /// <returns>最终执行结果</returns>
        public SkillPhaseResult Execute(ref SkillContext ctx, SkillExecutionToken token)
        {
            foreach (var phase in _phases)
            {
                // 检查是否已取消
                if (token.IsCancelled)
                    return SkillPhaseResult.Cancel;

                var result = phase.Execute(ref ctx, token);

                // Fail：正常终止（如无目标、能量不足）
                if (result == SkillPhaseResult.Fail)
                    return SkillPhaseResult.Fail;

                // Cancel：异常终止
                if (result == SkillPhaseResult.Cancel)
                    return SkillPhaseResult.Cancel;
            }

            return SkillPhaseResult.Continue;
        }
    }
}
