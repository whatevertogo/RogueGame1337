using System.Collections.Generic;
using UnityEngine;
using Character.Core;

namespace CardSystem.SkillSystem.Execution
{
    /// <summary>
    /// 直接伤害执行模块：对 SkillContext 中的目标造成固定伤害，并可选地为目标附加 SkillEffectSO 列表中的状态效果。
    /// 兼容性：
    /// - 如果 ctx.Targets 为空但 ctx.ExplicitTarget 不为空，将把 ExplicitTarget 作为单体目标。
    /// - 实现应对 null/空安全返回。
    /// </summary>
    [CreateAssetMenu(menuName = "Card System/Execution/Direct Damage")]
    public class DirectDamageExecutionModuleSO : ExecutionModuleSO
    {
        [Header("Damage")]
        [Tooltip("基础伤害数值（每个目标）")]
        public float damage = 10f;

        [Tooltip("击退力量（可为 0）")]
        public float knockbackForce = 0f;

        [Header("Effects (optional)")]
        [Tooltip("在对目标造成伤害后附加的状态效果（每个 effect 会被实例化一次并通过 SkillContext.ApplyStatusEffect 应用）")]
        public List<SkillEffectSO> Effects = new List<SkillEffectSO>();

        [Header("VFX (optional)")]
        [Tooltip("命中时播放的预制体（可选）")]
        public GameObject hitVfxPrefab;
        [Tooltip("命中 VFX 存活时间（秒）")]
        public float hitVfxLifetime = 1.5f;

        public override void Execute(SkillContext ctx)
        {
            if (ctx == null) return;

            // 若没有通过 targeting 填充 Targets，但存在 ExplicitTarget，则把它作为单体目标
            if ((ctx.Targets == null || ctx.Targets.Count == 0) && ctx.ExplicitTarget != null)
            {
                ctx.Targets.Add(ctx.ExplicitTarget);
            }

            if (ctx.Targets == null || ctx.Targets.Count == 0) return;

            foreach (var target in ctx.Targets)
            {
                if (target == null) continue;

                // 组装 DamageInfo
                var info = DamageInfo.Create(damage);
                info.Source = ctx.Owner != null ? ctx.Owner.gameObject : null;
                info.HitPoint = target.transform.position;

                // knockback dir: from attacker (owner position or context position) to target
                Vector2 origin = ctx.Owner != null ? (Vector2)ctx.Owner.position : ctx.Position;
                var dir = (target.transform.position - (Vector3)origin);
                info.KnockbackDir = dir.sqrMagnitude > 0.0001f ? ((Vector2)dir).normalized : Vector2.zero;
                info.KnockbackForce = knockbackForce;

                // 应用伤害
                ctx.ApplyDamage(target, info);

                // 应用状态效果（每个 effectSO 都会生成一个 IStatusEffect 实例）
                if (Effects != null && Effects.Count > 0)
                {
                    var effectInstances = new List<Character.Effects.IStatusEffect>();
                    foreach (var so in Effects)
                    {
                        if (so == null) continue;
                        var eff = so.CreateEffect();
                        if (eff != null) effectInstances.Add(eff);
                    }

                    if (effectInstances.Count > 0)
                    {
                        ctx.ApplyStatusEffect(target, effectInstances.ToArray());
                    }
                }

                // 播放命中 VFX（若配置）
                // TODO-配置VFX
                if (hitVfxPrefab != null)
                {
                    var tv = Object.Instantiate(hitVfxPrefab, target.transform.position, Quaternion.identity);
                    Object.Destroy(tv, Mathf.Max(0.1f, hitVfxLifetime));
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (damage < 0f) damage = 0f;
            if (knockbackForce < 0f) knockbackForce = 0f;
            if (hitVfxLifetime < 0.1f) hitVfxLifetime = 0.1f;
            if (Effects == null) Effects = new List<SkillEffectSO>();
        }
#endif
    }
}
