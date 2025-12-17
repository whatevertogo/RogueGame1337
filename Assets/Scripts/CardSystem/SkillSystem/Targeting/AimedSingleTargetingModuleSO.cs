using System.Collections.Generic;
using UnityEngine;
using CardSystem.SkillSystem;
using CardSystem.SkillSystem.Enum;

namespace CardSystem.SkillSystem.Targeting
{
    /// <summary>
    /// Aimed single target selection module.
    /// - 使用 aimPoint（或 SkillContext.Position/AimDirection）作为中心，
    /// - 在给定的小半径内查找符合条件的目标并返回其中最合适的一个（例如最靠近瞄点的目标）。
    /// - 非交互式（RequiresManualSelection == false）。
    /// </summary>
    [CreateAssetMenu(menuName = "Card System/Targeting/Aimed Single")]
    public class AimedSingleTargetingModuleSO : TargetingModuleSO
    {
        [Header("Aimed Single Target Settings")]
        [Tooltip("最大瞄准距离（当没有提供 aimPoint 时，会用 Owner.position + AimDirection * range 作为参考点）")]
        public float range = 12f;

        [Tooltip("在瞄点周围搜索目标的半径（越大越容易命中附近目标）")]
        public float selectionRadius = 0.6f;

        [Tooltip("用于检测目标的 LayerMask（与 Physics2D 查询配合）")]
        public LayerMask targetMask;

        [Tooltip("目标阵营选择（Hostile/Friendly/All），默认选敌对")]
        public TargetTeam targetTeam = TargetTeam.Hostile;

        [Tooltip("是否排除施法者自身（通常为 true）")]
        public bool excludeSelf = true;

        [Tooltip("当存在多个候选目标时，是否优先选择距离瞄点最近的目标（否则取第一个）")]
        public bool preferClosestToAim = true;

        public override int AcquireTargets(SkillContext ctx, Vector3? aimPoint = null)
        {
            if (ctx == null) return 0;

            ctx.Targets.Clear();

            // 计算瞄点（优先使用传入的 aimPoint）
            Vector2 centre;
            if (aimPoint.HasValue)
            {
                centre = aimPoint.Value;
            }
            else if (ctx.Owner != null)
            {
                centre = (Vector2)ctx.Owner.position + ctx.AimDirection.normalized * Mathf.Max(0.0001f, range);
            }
            else
            {
                centre = Vector2.zero;
            }

            // 构建队伍过滤器（使用 helper）
            var pred = TargetingHelper.BuildTeamPredicate(ctx.OwnerTeam, targetTeam, ctx.Owner != null ? ctx.Owner.gameObject : null, excludeSelf);

            // 使用现有的 AOE 帮助器以非分配方式获取候选（利用临时列表）
            var candidates = new List<GameObject>();
            TargetingHelper.GetAoeTargets(centre, selectionRadius, targetMask, candidates, pred);

            if (candidates.Count == 0)
                return 0;

            GameObject chosen = null;

            if (preferClosestToAim)
            {
                float bestDist2 = float.MaxValue;
                for (int i = 0; i < candidates.Count; i++)
                {
                    var go = candidates[i];
                    if (go == null) continue;
                    float d2 = ((Vector2)go.transform.position - centre).sqrMagnitude;
                    if (d2 < bestDist2)
                    {
                        bestDist2 = d2;
                        chosen = go;
                    }
                }
            }
            else
            {
                // 默认返回第一个（通常按物理查询顺序）
                chosen = candidates[0];
            }

            if (chosen != null)
            {
                ctx.Targets.Add(chosen);
                return 1;
            }

            return 0;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (selectionRadius < 0f) selectionRadius = 0f;
            if (range < 0f) range = 0f;
        }
#endif
    }
}
