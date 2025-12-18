using System.Collections.Generic;
using Character;
using Character.Components;
using UnityEngine;
using CardSystem.SkillSystem.Enum;

namespace CardSystem.SkillSystem
{
    [CreateAssetMenu(fileName = "AreaEffectExecutor", menuName = "Card System/Executors/Area Effect")]
    /// <summary>
    /// 范围效果执行器
    /// </summary>
    public class AreaEffectExecutor : SkillExecutorSO
    {
        [Header("AOE Settings")]
        [Tooltip("AOE 半径（世界单位）")]
        public float radius = 1f;

        [Tooltip("目标阵营过滤")]
        public TargetTeam targetTeam = TargetTeam.Hostile;

        [Tooltip("检测图层掩码")]
        public LayerMask detectionMask = default;

        public override void Execute(SkillDefinition skill, SkillContext ctx)
        {
            if (skill == null) return;

            Vector3 origin = ctx.AimPoint ?? (ctx.Caster != null ? ctx.Caster.transform.position : Vector3.zero);

            var foundTargets = new List<CharacterBase>();

            if (radius > 0f)
            {
                var hits = Physics2D.OverlapCircleAll(origin, radius);
                foreach (var h in hits)
                {
                    if (h == null) continue;
                    if (((1 << h.gameObject.layer) & detectionMask.value) == 0) continue;

                    var cb = h.GetComponent<CharacterBase>();
                    if (cb == null) continue;

                    var health = cb.GetComponent<HealthComponent>();
                    if (health != null && health.IsDead) continue;

                    // 阵营过滤
                    if (targetTeam == TargetTeam.Hostile && ctx.Caster != null && cb.Team == ctx.Caster.Team) continue;
                    if (targetTeam == TargetTeam.Friendly && ctx.Caster != null && cb.Team != ctx.Caster.Team) continue;

                    foundTargets.Add(cb);
                }
            }
            else
            {
                var hit = Physics2D.OverlapPoint(origin);
                if (hit != null)
                {
                    var cb = hit.GetComponent<CharacterBase>();
                    if (cb != null) foundTargets.Add(cb);
                }
            }

            // 应用效果（从 Definition 创建实例）
            foreach (var target in foundTargets)
            {
                var effectComp = target.GetComponent<StatusEffectComponent>();
                if (effectComp == null) continue;
                foreach (var def in skill.Effects)
                {
                    if (def == null) continue;
                    var inst = def.CreateInstance();
                    if (inst == null) continue;
                    effectComp.AddEffect(inst);
                }
            }
        }
    }
}
