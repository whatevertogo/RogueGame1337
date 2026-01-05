using Character;
using Character.Components;
using Character.Player.Skill.Core;
using Character.Player.Skill.Targeting;
namespace Character.Player.Skill.Pipeline.Phases
{

    /// <summary>
    /// 效果应用阶段：从运行时缓存获取效果 → 遍历目标 → 创建实例 → 应用
    /// 重构：使用缓存机制，避免每次施放时重新计算效果列表
    /// </summary>
    public sealed class EffectApplicationPhase : ISkillPhase
    {
        private readonly EffectFactory _effectFactory;

        public string PhaseName => "EffectApplication";

        public EffectApplicationPhase(EffectFactory effectFactory)
        {
            _effectFactory = effectFactory;
        }

        public SkillPhaseResult Execute (ref SkillContext ctx, SkillExecutionToken token)
        {
            if (token.IsCancelled) return SkillPhaseResult.Cancel;

            var runtime = ctx.Runtime;
            var targets = ctx.TargetResult.Targets;
            var caster = ctx.Caster;

            // 从运行时缓存获取所有效果（基础 + 修改器生成）
            var allEffects = runtime.GetAllEffects();
            if (allEffects == null || allEffects.Count == 0)
                return SkillPhaseResult.Continue; // 没有效果不是失败

            foreach (var target in targets)
            {
                if (target == null) continue;

                var statusComp = target.GetComponent<StatusEffectComponent>();
                if (statusComp == null) continue;

                // 应用所有效果
                foreach (var effectDef in allEffects)
                {
                    if (effectDef == null) continue;

                    var effectInstance = _effectFactory.CreateInstance(effectDef, caster);
                    if (effectInstance == null) continue;

                    // 传递真伤标记
                    effectInstance.IsTrueDamage = ctx.DamageResult.IsTrueDamage;

                    statusComp.AddEffect(effectInstance);
                }
            }

            return SkillPhaseResult.Continue;
        }
    }
}
