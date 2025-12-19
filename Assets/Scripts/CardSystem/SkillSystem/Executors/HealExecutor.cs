using Character.Components;
using UnityEngine;
using CardSystem.SkillSystem.Enum;

namespace CardSystem.SkillSystem
{
    /// <summary>
    /// 治疗执行器 - 为目标恢复生命值
    /// </summary>
    [CreateAssetMenu(fileName = "HealExecutor", menuName = "Card System/Executors/Heal")]
    public class HealExecutor : SkillExecutorSO
    {
        [Header("治疗设置")]
        [Tooltip("基础治疗量")]
        public float baseHealAmount = 20f;
        
        [Tooltip("是否基于施法者最大生命值百分比")]
        public bool useMaxHPPercent = false;
        
        [Tooltip("最大生命值百分比（当 useMaxHPPercent=true 时生效）")]
        [Range(0f, 1f)]
        public float maxHPPercent = 0.1f;

        [Header("目标设置")]
        [Tooltip("目标类型")]
        public TargetTeam targetTeam = TargetTeam.Self;
        
        [Tooltip("检测范围（<=0 表示使用 ctx.Targets）")]
        public float detectionRadius = 0f;
        
        [Tooltip("检测图层")]
        public LayerMask detectionMask = default;

        public override void Execute(SkillDefinition skill, SkillContext ctx)
        {
            var targets = new System.Collections.Generic.List<Character.CharacterBase>();

            // 确定目标
            if (targetTeam == TargetTeam.Self)
            {
                if (ctx.Caster != null) targets.Add(ctx.Caster);
            }
            else if (detectionRadius > 0f)
            {
                // 范围检测
                Vector3 origin = ctx.Caster != null ? ctx.Caster.transform.position : Vector3.zero;
                var hits = Physics2D.OverlapCircleAll(origin, detectionRadius);

                foreach (var h in hits)
                {
                    if (h == null) continue;
                    if (((1 << h.gameObject.layer) & detectionMask.value) == 0) continue;

                    var cb = h.GetComponent<Character.CharacterBase>();
                    if (cb == null) continue;

                    var health = cb.GetComponent<HealthComponent>();
                    if (health != null && health.IsDead) continue;

                    // 阵营过滤
                    bool isSameTeam = ctx.Caster != null && cb.Team == ctx.Caster.Team;
                    if (targetTeam == TargetTeam.Friendly && !isSameTeam) continue;

                    targets.Add(cb);
                }
            }
            else
            {
                // 使用上下文传入的目标
                if (ctx.Targets != null)
                {
                    foreach (var t in ctx.Targets)
                    {
                        if (t != null) targets.Add(t);
                    }
                }
            }

            // 计算治疗量
            float healAmount = baseHealAmount;
            if (useMaxHPPercent && ctx.Caster != null)
            {
                var casterStats = ctx.Caster.GetComponent<CharacterStats>();
                if (casterStats != null)
                {
                    healAmount = casterStats.MaxHP.Value * maxHPPercent;
                }
            }

            // 应用治疗
            foreach (var target in targets)
            {
                var stats = target.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    stats.Heal(healAmount);
                }
            }
        }
    }
}
