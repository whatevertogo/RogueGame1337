using Character.Player.Skill.Core;
using Character.Player.Skill.Targeting;

namespace Character.Player.Skill.Pipeline.Phases
{
    public class VFXPhase : ISkillPhase
    {
        public string PhaseName => "VFXPhase";

        public SkillPhaseResult Execute(SkillContext ctx, SkillExecutionToken token)
        {
            if (token.IsCancelled) return SkillPhaseResult.Cancel;
            var vfxPrefab = ctx.Runtime.Skill.vfxPrefab;
            if (vfxPrefab != null)
            {
                VFXSystem.Instance.PlayAt(vfxPrefab, ctx.CasterPosition);
            }

            return SkillPhaseResult.Continue;
        }
    }
}