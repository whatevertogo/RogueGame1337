using System;
using System.Collections.Generic;
using UnityEngine;
using Character.Interfaces;
using Character;

namespace CardSystem.SkillSystem.Targeting
{
    /// <summary>
    /// 技能目标选择辅助工具。
    /// - 使用 Physics.OverlapSphereNonAlloc 减少分配
    /// - 支持外部过滤回调（例如阵营/状态过滤）
    /// </summary>
    public static class TargetingHelper
    {
        private static readonly Collider[] s_buffer = new Collider[64];

        /// <summary>
        /// 获取 AOE 目标并填充到 outTargets（会 Clear）
        /// 返回匹配目标数量
        /// </summary>
        public static int GetAoeTargets(Vector3 centre, float radius, LayerMask mask, List<GameObject> outTargets, Func<GameObject, bool> predicate = null)
        {
            if (outTargets == null) throw new ArgumentNullException(nameof(outTargets));
            outTargets.Clear();

            int count = Physics.OverlapSphereNonAlloc(centre, radius, s_buffer, mask);
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
    }
}
