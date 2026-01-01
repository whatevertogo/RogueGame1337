using Character.Player.Skill.Core;
using Character.Player.Skill.Targeting;
namespace Character.Player.Skill.Pipeline.Phases
{

    /// <summary>
    /// 伤害计算阶段：应用跨阶段修改器 → 应用伤害修改器
    /// </summary>
    public sealed class DamageCalculationPhase : ISkillPhase
    {
        public string PhaseName => "DamageCalculation";

        public SkillPhaseResult Execute(SkillContext ctx, SkillExecutionToken token)
        {
            if (token.IsCancelled) return SkillPhaseResult.Cancel;

            var rt = ctx.Runtime;

            // 1. 应用跨阶段修改器
            rt.ApplyCrossPhaseModifiers(ref ctx);

            // 2. 应用伤害修改器
            rt.ApplyDamageModifiers(ref ctx.DamageResult);

            return SkillPhaseResult.Continue;
        }
    }
}
