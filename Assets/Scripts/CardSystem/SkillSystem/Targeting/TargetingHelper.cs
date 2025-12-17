using System;
using System.Collections.Generic;
using UnityEngine;
using Character.Interfaces;
using Character;
using CardSystem.SkillSystem.Enum;

namespace CardSystem.SkillSystem.Targeting
{
    /// <summary>
    /// 技能目标选择辅助工具。
    /// - 使用 Physics2D.OverlapCircleNonAlloc 减少分配
    /// - 支持外部过滤回调（例如阵营/状态过滤）
    /// </summary>
    public static class TargetingHelper
    {
        private static readonly Collider2D[] s_buffer = new Collider2D[128];

        /// <summary>
        /// 获取 AOE 目标并填充到 outTargets（会 Clear）
        /// 返回匹配目标数量
        /// </summary>
        public static int GetAoeTargets(Vector3 centre, float radius, LayerMask mask, List<GameObject> outTargets, Func<GameObject, bool> predicate = null)
        {
            if (outTargets == null) throw new ArgumentNullException(nameof(outTargets));
            outTargets.Clear();

            // Use Physics2D to match the project's 2D colliders (Collider2D)
            int layerMask = mask.value;
            int count = Physics2D.OverlapCircleNonAlloc((Vector2)centre, radius, s_buffer, layerMask);

            if (count == s_buffer.Length)
            {
                Debug.LogWarning("[TargetingHelper] Overlap buffer full - consider increasing buffer size.");
            }

            for (int i = 0; i < count; i++)
            {
                var col = s_buffer[i];
                if (col == null) continue;
                var go = col.gameObject;
                if (go == null) continue;
                if (predicate != null && !predicate(go)) continue;
                outTargets.Add(go);
            }

          return outTargets.Count;
        }

        /// <summary>
        /// 简单的敌对过滤器：返回 true 表示目标为敌对（非同阵营）
        /// 如果目标没有实现 ITeamMember，则返回 false（视为不可选）
        /// </summary>
        public static bool IsHostileTo(TeamType ownerTeam, GameObject target)
        {
            if (target == null) return false;
            var tm = target.GetComponent<ITeamMember>();
            if (tm == null) return false;
            return tm.Team != ownerTeam;
        }

        /// <summary>
        /// 根据 TargetTeam 构建过滤器。owner 可为 null；excludeSelf 表示排除施法者自身。
        /// </summary>
        public static Func<GameObject, bool> BuildTeamPredicate(TeamType ownerTeam, TargetTeam targetTeam, GameObject owner = null, bool excludeSelf = false)
        {
            return go =>
            {
                if (go == null) return false;
                if (excludeSelf && owner != null && go == owner) return false;
                if (targetTeam == TargetTeam.All) return true;
                var tm = go.GetComponent<ITeamMember>();
                if (tm == null) return false;
                if (targetTeam == TargetTeam.Hostile) return tm.Team != ownerTeam;
                if (targetTeam == TargetTeam.Friendly) return tm.Team == ownerTeam;
                return false;
            };
        }
    }
}
