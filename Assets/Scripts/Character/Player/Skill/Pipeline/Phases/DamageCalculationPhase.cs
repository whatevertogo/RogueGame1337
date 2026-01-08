using Character.Player.Skill.Core;
using Character.Player.Skill.Targeting;

namespace Character.Player.Skill.Pipeline.Phases
{
    /// <summary>
    /// 伤害计算阶段：应用伤害修改器
    /// 跨阶段修改器已移至独立的 CrossPhase
    /// </summary>
    public sealed class DamageCalculationPhase : ISkillPhase
    {
        public string PhaseName => "DamageCalculation";

        public SkillPhaseResult Execute(SkillContext ctx, SkillExecutionToken token)
        {
            if (token.IsCancelled) return SkillPhaseResult.Cancel;

            var rt = ctx.Runtime;

            // 应用伤害修改器
            rt.ApplyDamageModifiers(ctx.DamageResult);

            return SkillPhaseResult.Continue;
        }
    }
}
