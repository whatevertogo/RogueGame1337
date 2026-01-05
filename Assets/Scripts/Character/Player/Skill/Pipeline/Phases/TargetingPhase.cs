namespace Character.Player.Skill.Pipeline.Phases
{
    using Character;
    using Character.Player.Skill.Core;
    using Character.Player.Skill.Targeting;
    using System.Collections.Generic;

    /// <summary>
    /// 目标获取阶段：应用修改器 → 播放 VFX → 获取目标 → 过滤
    /// </summary>
    public sealed class TargetingPhase : ISkillPhase
    {
        public string PhaseName => "Targeting";

        public SkillPhaseResult Execute( SkillContext ctx, SkillExecutionToken token)
        {
            if (token.IsCancelled) return SkillPhaseResult.Cancel;

            var rt = ctx.Runtime;
            var def = rt.Skill;

            // 1. 应用修改器
            rt.ApplyTargetingModifiers(ref ctx.Targeting);

            // 2. 播放 VFX
            if (def.vfxPrefab != null)
            {
                UnityEngine.Object.Instantiate(def.vfxPrefab, ctx.CasterPosition, UnityEngine.Quaternion.identity);
            }

            // 3. 获取目标
            var targets = def.TargetAcquireSO != null
                ? def.TargetAcquireSO.Acquire(ctx)
                : new List<CharacterBase>();

            // 4. 过滤目标（倒序遍历删除，支持 ref 参数）
            if (def.TargetFilters != null && def.TargetFilters.filters != null && def.TargetFilters.filters.Count > 0)
            {
                for (int i = targets.Count - 1; i >= 0; i--)
                {
                    if (!def.TargetFilters.IsValid(ctx, targets[i]))
                    {
                        targets.RemoveAt(i);
                    }
                }
            }

            // 5. 检查是否有有效目标
            if (targets == null || targets.Count == 0)
                return SkillPhaseResult.Fail;

            // 6. 保存结果（通过 ref 修改，后续 Phase 可看到）
            ctx.TargetResult = new TargetResult
            {
                Targets = targets,
                Point = ctx.AimPoint
            };

            return SkillPhaseResult.Continue;
        }
    }
}
