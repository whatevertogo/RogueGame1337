using Character.Components;
using UnityEngine;

namespace CardSystem.SkillSystem
{
    [CreateAssetMenu(fileName = "InstantBuffForExecutor", menuName = "Card System/Executors/Instant Buff")]
    /// <summary>
    /// 使用执行器这个得自己传targets
    /// </summary>
    public class InstantBuffExecutor : SkillExecutorSO
    {
        public override void Execute(SkillDefinition skill, SkillContext ctx)
        {
            if (skill == null || ctx.Targets == null) return;

            foreach (var target in ctx.Targets)
            {
                if (target == null) continue;
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
