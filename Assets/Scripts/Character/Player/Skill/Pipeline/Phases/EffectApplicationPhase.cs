using Character;
using Character.Components;
using Character.Player.Skill.Core;
using Character.Player.Skill.Targeting;
namespace Character.Player.Skill.Pipeline.Phases
{

    /// <summary>
    /// 效果应用阶段：遍历目标 → 创建效果 → 应用
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

            var def = ctx.Runtime.Skill;
            var targets = ctx.TargetResult.Targets;
            var caster = ctx.Caster;

            // 获取所有效果（基础 + 进化分支），即时计算
            var allEffects = def.GetAllEffects(ctx.Runtime);
            if (allEffects == null || allEffects.Count == 0)
                return SkillPhaseResult.Continue; // 没有效果不是失败

            foreach (var target in targets)
            {
                if (target == null) continue;

                var statusComp = target.GetComponent<StatusEffectComponent>();
                if (statusComp == null) continue;

                // 应用所有效果（包含基础效果和分支效果）
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
