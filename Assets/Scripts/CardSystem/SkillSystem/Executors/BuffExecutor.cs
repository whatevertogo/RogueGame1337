using Character.Components;
using UnityEngine;
using CardSystem.SkillSystem.Enum;

namespace CardSystem.SkillSystem
{
    /// <summary>
    /// Buff 执行器 - 给目标应用状态效果
    /// 支持对自己、友方或敌方应用 Buff/Debuff
    /// </summary>
    [CreateAssetMenu(fileName = "BuffExecutor", menuName = "Card System/Executors/Buff")]
    public class BuffExecutor : SkillExecutorSO
    {
        [Header("目标设置")]
        [Tooltip("目标类型：Self(自己) / Friendly(友方) / Hostile(敌方)")]
        public TargetTeam targetTeam = TargetTeam.Self;
        
        [Tooltip("是否包含施法者自己（仅当 targetTeam != Self 时有效）")]
        public bool includeCaster = false;

        [Header("检测设置（用于自动查找目标）")]
        [Tooltip("检测范围（<=0 表示使用 ctx.Targets）")]
        public float detectionRadius = 0f;
        
        [Tooltip("检测图层")]
        public LayerMask detectionMask = default;

        public override void Execute(SkillDefinition skill, SkillContext ctx)
        {
            if (skill == null || skill.Effects == null || skill.Effects.Count == 0) return;

            var targets = new System.Collections.Generic.List<Character.CharacterBase>();

            // 根据目标类型确定目标列表
            if (targetTeam == TargetTeam.Self)
            {
                // 只对自己应用
                if (ctx.Caster != null) targets.Add(ctx.Caster);
            }
            else if (detectionRadius > 0f)
            {
                // 自动检测范围内的目标
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
                    if (targetTeam == TargetTeam.Hostile && isSameTeam) continue;
                    if (targetTeam == TargetTeam.Friendly && !isSameTeam) continue;
                    // All 不过滤
                    
                    // 是否包含施法者
                    if (!includeCaster && cb == ctx.Caster) continue;

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

            // 应用效果
            foreach (var target in targets)
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
