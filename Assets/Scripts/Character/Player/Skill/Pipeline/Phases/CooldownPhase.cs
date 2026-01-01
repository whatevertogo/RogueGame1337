using Character.Player.Skill.Core;
using Character.Player.Skill.Targeting;
namespace Character.Player.Skill.Pipeline.Phases
{

    /// <summary>
    /// 冷却阶段：应用冷却修改器
    /// </summary>
    public sealed class CooldownPhase : ISkillPhase
    {
        public string PhaseName => "Cooldown";

        public SkillPhaseResult Execute(SkillContext ctx, SkillExecutionToken token)
        {
            if (token.IsCancelled) return SkillPhaseResult.Cancel;

            // 应用冷却修改器
            ctx.Runtime.ApplyCooldownModifiers();

            return SkillPhaseResult.Continue;
        }
    }
}
