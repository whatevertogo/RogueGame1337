using UnityEngine;
using CardSystem.SkillSystem;
using CardSystem.SkillSystem.Enum;

namespace CardSystem.SkillSystem.Targeting
{
    /// <summary>
    /// Self / Self-centered AOE targeting module.
    /// - 如果 radius <= 0：把施法者自身作为唯一目标（Self）。
    /// - 如果 radius > 0：以施法者为中心做 AOE，返回符合目标过滤器的目标（SelfTarget 风格，可排除自身）。
    /// </summary>
    [CreateAssetMenu(menuName = "Card System/Targeting/Self Targeting")]
    public class SelfTargetingModuleSO : TargetingModuleSO
    {
        [Header("Self Targeting")]
        [Tooltip("半径 <= 0 表示仅选中自身；>0 则作为 AOE 半径")]
        public float radius = 0f;

        [Tooltip("AOE 搜索时使用的 LayerMask")]
        public LayerMask targetMask;

        [Tooltip("目标阵营（Hostile / Friendly / All）")]
        public TargetTeam targetTeam = TargetTeam.Hostile;

        [Tooltip("在 AOE 模式下是否排除施法者自身（通常用于治疗/增益盟友时设置为 true）")]

        public bool excludeSelf = true;
        /// <summary>
        /// 非交互式采集：返回填充后的目标数量
        /// </summary>
        public override int AcquireTargets(SkillContext ctx, Vector3? aimPoint = null)
        {
            if (ctx == null) return 0;

            ctx.Targets.Clear();

            // radius <= 0: 单体（自身）
            if (radius <= 0f)
            {
                if (ctx.Owner != null)
                {
                    ctx.Targets.Add(ctx.Owner.gameObject);
                    return 1;
                }
                return 0;
            }

            // AOE 以施法者为中心
            var centre = ctx.Owner != null ? (Vector2)ctx.Owner.position : ctx.Position;
            ctx.Position = centre;

            var pred = TargetingHelper.BuildTeamPredicate(ctx.OwnerTeam, targetTeam, ctx.Owner != null ? ctx.Owner.gameObject : null, excludeSelf);
            return TargetingHelper.GetAoeTargets(centre, radius, targetMask, ctx.Targets, pred);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (radius < 0f) radius = 0f;
        }
#endif
    }
}
