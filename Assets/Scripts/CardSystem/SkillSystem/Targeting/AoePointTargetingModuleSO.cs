using UnityEngine;
using CardSystem.SkillSystem.Enum;

namespace CardSystem.SkillSystem.Targeting
{
    /// <summary>
    /// 范围点目标选择模块（AOE Point Targeting Module）。
    /// 该模块允许技能在指定点周围的区域内选择多个目标。
    /// </summary>
    [CreateAssetMenu(menuName = "Card System/Targeting/AOE Point")]
    public class AoePointTargetingModuleSO : TargetingModuleSO
    {
        [Header("AOE Settings")]
        public float radius = 3f;
        public LayerMask targetMask;
        public TargetTeam targetTeam = TargetTeam.Hostile;
        [Tooltip("排除施法者自身（通常用于治疗盟友等场景）")]
        public bool excludeSelf = false;

        public override int AcquireTargets(SkillContext ctx, Vector3? aimPoint = null)
        {
            if (ctx == null) return 0;
            ctx.Targets.Clear();

            var centre = aimPoint.HasValue ? (Vector2)aimPoint.Value : ctx.Position;
            var pred = TargetingHelper.BuildTeamPredicate(ctx.OwnerTeam, targetTeam, ctx.Owner != null ? ctx.Owner.gameObject : null, excludeSelf);
            return TargetingHelper.GetAoeTargets(centre, radius, targetMask, ctx.Targets, pred);
        }
    }
}
