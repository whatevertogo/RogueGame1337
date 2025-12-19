using Character.Components;
using Character.Core;
using UnityEngine;
using CardSystem.SkillSystem.Enum;

namespace CardSystem.SkillSystem
{
    /// <summary>
    /// 伤害执行器 - 对目标造成伤害
    /// 支持固定伤害、基于施法者属性的伤害等
    /// </summary>
    [CreateAssetMenu(fileName = "DamageExecutor", menuName = "Card System/Executors/Damage")]
    public class DamageExecutor : SkillExecutorSO
    {
        [Header("伤害设置")]
        [Tooltip("基础伤害值")]
        public float baseDamage = 10f;
        
        [Tooltip("是否基于施法者攻击力（true: 伤害=baseDamage+攻击力*attackPowerMultiplier）")]
        public bool useAttackPower = true;
        
        [Tooltip("攻击力倍率")]
        public float attackPowerMultiplier = 1f;

        [Header("目标设置")]
        [Tooltip("目标阵营过滤")]
        public TargetTeam targetTeam = TargetTeam.Hostile;
        
        [Tooltip("检测范围（<=0 表示使用 ctx.Targets）")]
        public float detectionRadius = 0f;
        
        [Tooltip("检测图层")]
        public LayerMask detectionMask = default;

        public override void Execute(SkillDefinition skill, SkillContext ctx)
        {
            var targets = new System.Collections.Generic.List<Character.CharacterBase>();

            // 确定目标
            if (detectionRadius > 0f)
            {
                // 范围检测
                Vector3 origin = ctx.AimPoint ?? (ctx.Caster != null ? ctx.Caster.transform.position : Vector3.zero);
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
                    if (targetTeam == TargetTeam.Hostile && isSameTeam) continue;
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

            // 计算伤害
            float damage = baseDamage;
            if (useAttackPower && ctx.Caster != null)
            {
                var casterStats = ctx.Caster.GetComponent<CharacterStats>();
                if (casterStats != null)
                {
                    damage += casterStats.AttackPower.Value * attackPowerMultiplier;
                }
            }

            // 应用伤害
            foreach (var target in targets)
            {
                var stats = target.GetComponent<CharacterStats>();
                if (stats == null) continue;

                // 创建伤害信息
                var damageInfo = DamageInfo.Create(damage);
                damageInfo.Source = ctx.Caster != null ? ctx.Caster.gameObject : null;

                // 应用伤害
                stats.TakeDamage(damageInfo);
            }
        }
    }
}
