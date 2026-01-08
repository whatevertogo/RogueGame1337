
using Character.Player.Skill.Core;
using Character.Player.Skill.Targeting;

namespace Character.Player.Skill.Pipeline.Phases
{
    /// <summary>
    /// 跨阶段修改器应用阶段
    /// 在目标获取完成后、效果应用前执行
    /// 用于处理需要访问多个阶段数据的修改器逻辑
    /// 例如："如果目标数量 > 3，则伤害增加50%"
    /// </summary>
    public sealed class CrossPhase : ISkillPhase
    {
        public string PhaseName => "CrossPhase";

        public SkillPhaseResult Execute(SkillContext ctx, SkillExecutionToken token)
        {
            if (token.IsCancelled) return SkillPhaseResult.Cancel;

            var rt = ctx.Runtime;
            rt.ApplyCrossPhaseModifiers(ctx);

            return SkillPhaseResult.Continue;
        }
    }
}
