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
            var VFXName = ctx.Runtime.Skill.vfxPrefabName;

            var targets = ctx.TargetResult.Targets;
            if (!string.IsNullOrEmpty(VFXName))
            {
                foreach (var target in targets)
                {
                    VFXSystem.Instance.PlayAt(VFXName, target.transform.position);
                }
            }

            return SkillPhaseResult.Continue;
        }
    }
}