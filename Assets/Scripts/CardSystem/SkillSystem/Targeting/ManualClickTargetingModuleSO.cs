using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardSystem.SkillSystem;
using CardSystem.SkillSystem.Enum;

namespace CardSystem.SkillSystem.Targeting
{
    /// <summary>
    /// 手动点击选目标模块（交互式）。
    /// - RequiresManualSelection = true，PlayerSkillComponent 会等待此协程完成；
    /// - 协程期间会发布 InteractionPromptEvent 以提示玩家（可被 UI 层捕获显示提示）。
    /// - 选中目标后会把结果写入 ctx.ExplicitTarget（同时将 ctx.Targets 清空并添加该目标，便于兼容各类 Execution 模块）。
    /// </summary>
    [CreateAssetMenu(menuName = "Card System/Targeting/Manual Click")]
    public class ManualClickTargetingModuleSO : TargetingModuleSO
    {
        [Header("Manual Click Settings")]
        [Tooltip("鼠标点击检测的半径（用于更容易点击到小目标）")]
        public float selectionRadius = 0.6f;

        [Tooltip("当在空白处点击时，搜索最近目标的容忍半径")]
        public float fallbackSearchRadius = 1.2f;

        [Tooltip("允许的目标 LayerMask")]
        public LayerMask targetMask = Physics2D.DefaultRaycastLayers;

        [Tooltip("目标阵营过滤（Hostile/Friendly/All）")]
        public TargetTeam targetTeam = TargetTeam.Hostile;

        [Tooltip("在 AOE/筛选时是否排除施法者自身")]
        public bool excludeSelf = true;

        [Tooltip("允许通过右键或 ESC 取消选择")]
        public bool allowCancel = true;

        [Tooltip("选择最大等待时间（秒），<=0 表示无超时）")]
        public float timeout = 30f;

        [Tooltip("交互提示文字（发布给全局 EventBus，UI 层可订阅 InteractionPromptEvent 展示）")]
        public string promptMessage = "Select target: Left Click to confirm, Right Click/Esc to cancel";

        public override bool RequiresManualSelection => true;

        public override int AcquireTargets(SkillContext ctx, Vector3? aimPoint = null)
        {
            // Manual module 不在此处做即时采集，使用 ManualSelectionCoroutine 进行交互式采集
            return 0;
        }

        /// <summary>
        /// 手动选择协程：驱动玩家点击选择目标
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public override IEnumerator ManualSelectionCoroutine(SkillContext ctx)
        {
            if (ctx == null) yield break;

            // 清理上下文预设
            ctx.Targets.Clear();
            ctx.ExplicitTarget = null;

            // 发布提示（UI 可订阅 InteractionPromptEvent）
            try
            {
                EventBus.Publish(new RogueGame.Events.InteractionPromptEvent { Message = promptMessage, Show = true });
            }
            catch { }

            float start = Time.time;
            GameObject selected = null;

            // 交互循环
            while (true)
            {
                // 超时判断
                if (timeout > 0f && Time.time - start > timeout) break;

                // 鼠标世界坐标
                Vector3 mouseScreen = UnityEngine.Input.mousePosition;
                Vector3 worldPos3 = Camera.main != null ? Camera.main.ScreenToWorldPoint(mouseScreen) : (Vector3)ctx.Position;
                worldPos3.z = 0f;
                Vector2 mouseWorld = worldPos3;

                // 在 cursor 附近查找目标（使用 small overlap）
                var hits = Physics2D.OverlapCircleAll(mouseWorld, selectionRadius, targetMask);
                GameObject hover = null;
                if (hits != null && hits.Length > 0)
                {
                    var pred = TargetingHelper.BuildTeamPredicate(ctx.OwnerTeam, targetTeam, ctx.Owner != null ? ctx.Owner.gameObject : null, excludeSelf);
                    foreach (var h in hits)
                    {
                        if (h == null) continue;
                        if (pred != null && !pred(h.gameObject)) continue;
                        hover = h.gameObject;
                        break;
                    }
                }

                // 鼠标左键点击：如果悬停到合法目标则选中；否则尝试以附近目标作为后备
                if (UnityEngine.Input.GetMouseButtonDown(0))
                {
                    if (hover != null)
                    {
                        selected = hover;
                        break;
                    }
                    else
                    {
                        // 尝试在较大半径内找最近的合法目标作为后备（例如点空地但附近有敌人）
                        var candidates = new List<GameObject>();
                        TargetingHelper.GetAoeTargets(mouseWorld, fallbackSearchRadius, targetMask, candidates,
                            TargetingHelper.BuildTeamPredicate(ctx.OwnerTeam, targetTeam, ctx.Owner != null ? ctx.Owner.gameObject : null, excludeSelf));
                        if (candidates.Count > 0)
                        {
                            // 取最近
                            float best = float.MaxValue;
                            GameObject bestGo = null;
                            foreach (var g in candidates)
                            {
                                if (g == null) continue;
                                float d = (g.transform.position - (Vector3)mouseWorld).sqrMagnitude;
                                if (d < best) { best = d; bestGo = g; }
                            }
                            if (bestGo != null)
                            {
                                selected = bestGo;
                                break;
                            }
                        }
                    }
                }

                // 右键或 ESC 取消
                if (allowCancel && (UnityEngine.Input.GetMouseButtonDown(1) || UnityEngine.Input.GetKeyDown(KeyCode.Escape)))
                {
                    break;
                }

                yield return null;
            }

            // 取消提示
            try
            {
                EventBus.Publish(new RogueGame.Events.InteractionPromptEvent { Message = "", Show = false });
            }
            catch { }

            // 如果选中了目标则写入 ctx 并结束
            if (selected != null)
            {
                ctx.ExplicitTarget = selected;
                ctx.Targets.Clear();
                ctx.Targets.Add(selected);
                yield break;
            }

            // 未选中或者取消
            ctx.ExplicitTarget = null;
            ctx.Targets.Clear();
            yield break;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (selectionRadius < 0f) selectionRadius = 0f;
            if (fallbackSearchRadius < 0f) fallbackSearchRadius = 0f;
            if (timeout < 0f) timeout = 0f;
        }
#endif
    }
}
