using System.Collections.Generic;
using System.Reflection;
using Character.Effects;
using UnityEngine;
using CardSystem.SkillSystem.Enum;


namespace CardSystem.SkillSystem
{
    [CreateAssetMenu(fileName = "NewSkillDefinition", menuName = "Card System/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        public string skillId;
        public Sprite icon;
        public float cooldown = 1f;
        [Tooltip("若大于0，技能触发后延迟检测目标并在延迟结束时应用效果（秒）")]
        public float detectionDelay = 0f;

        // Legacy fields kept for backward compatibility. New workflows should prefer modules (below).
        public TargetTeam targetTeam = TargetTeam.Hostile;
        public float range = 5f;      // 对单体/投射可用 (legacy)
        public float radius = 3f;     // AOE 半径 (legacy)
        public LayerMask targetMask;  // 哪些 layer 会被检测到 (legacy)
        public GameObject vfxPrefab;  // 可选

        [Header("效果列表 (legacy)")]
        [InlineEditor]
        public List<SkillEffectSO> Effects;

        [Header("Modular (preferred)")]
        [Tooltip("可选：目标选择模块（作为 ScriptableObject 引用）。若配置为 TargetingModuleSO 的实例，运行时会通过反射调用 AcquireTargets/ManualSelectionCoroutine；若留空则回退到 legacy 枚举逻辑。")]
        public ScriptableObject targetingModuleSO;
        [Tooltip("可选：执行模块（作为 ScriptableObject 引用）。若配置为 ExecutionModuleSO 的实例，运行时会通过反射调用 Execute；若留空则回退至 Effects 行为。")]
        public ScriptableObject executionModuleSO;

        /// <summary>
        /// Execute 支持模块化流程：
        /// 1) 若配置了 targetingModule 且 RequiresManualSelection 为 false，则直接调用 targetingModule.AcquireTargets 填充 context.Targets；
        ///    若 RequiresManualSelection 为 true，则应由调用方（PlayerSkillComponent）在交互结束后再走 Execute（交互协程负责填充 context.ExplicitTarget / Targets）。
        /// 2) 优先使用 executionModule.Execute 来处理目标；若未配置则回退为原有的 Effects 行为（对每个目标应用状态效果）。
        /// 3) 兼容性：若 context.Targets 为空但 context.ExplicitTarget 非空，执行阶段应把 ExplicitTarget 当作单体目标。
        /// </summary>
        public void Execute(SkillContext context, Vector3? aimPoint = null)
        {
            if (context == null) return;

            // Module-based targeting (runtime reflection):
            // 如果提供了 targetingModuleSO 且它是非交互式（RequiresManualSelection==false），则通过反射调用 AcquireTargets(context, aimPoint) 填充 Targets。
            // 若模块需要手动选择（RequiresManualSelection==true），调用方（例如 PlayerSkillComponent）应先跑模块的 ManualSelectionCoroutine 并把结果写入 context.ExplicitTarget/context.Targets。
            bool targetsAcquiredByModule = false;
            if (targetingModuleSO != null)
            {
                var mod = targetingModuleSO;
                var modType = mod.GetType();

                // 检查 RequiresManualSelection 属性（若存在）
                bool requiresManual = false;
                var requiresProp = modType.GetProperty("RequiresManualSelection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (requiresProp != null && requiresProp.PropertyType == typeof(bool))
                {
                    var val = requiresProp.GetValue(mod);
                    if (val is bool b) requiresManual = b;
                }

                if (!requiresManual)
                {
                    var acquireMethod = modType.GetMethod("AcquireTargets", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (acquireMethod != null)
                    {
                        try
                        {
                            // 调用模块的 AcquireTargets(context, aimPoint)
                            acquireMethod.Invoke(mod, new object[] { context, aimPoint });
                            targetsAcquiredByModule = true;
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[SkillDefinition] AcquireTargets invoke failed: {ex.Message}");
                        }
                    }
                }
                // 若 requiresManual==true，不在此处执行交互（由调用方负责）
            }

            if (!targetsAcquiredByModule)
            {
                // Legacy fallback: 使用原有的枚举逻辑（保持旧行为）
                context.Targets.Clear();

               
                    context.Position = centre;
                    var pred = CardSystem.SkillSystem.Targeting.TargetingHelper.BuildTeamPredicate(context.OwnerTeam, targetTeam, context.Owner != null ? context.Owner.gameObject : null, false);

                    context.Position = centre;
                    var pred2 = CardSystem.SkillSystem.Targeting.TargetingHelper.BuildTeamPredicate(context.OwnerTeam, targetTeam, context.Owner != null ? context.Owner.gameObject : null, true);
                    CardSystem.SkillSystem.Targeting.TargetingHelper.GetAoeTargets(centre, radius, targetMask, context.Targets, pred2);
                }
            }

            // If targets remain empty but an explicit target exists, use it as single target
            if ((context.Targets == null || context.Targets.Count == 0) && context.ExplicitTarget != null)
            {
                context.Targets.Add(context.ExplicitTarget);
            }

            // 执行阶段：优先使用 executionModuleSO（ScriptableObject，运行时通过反射调用 Execute）；若未配置则回退到 legacy Effects（主要用于状态效果）
            if (executionModuleSO != null)
            {
                var mod = executionModuleSO;
                var modType = mod.GetType();
                var execMethod = modType.GetMethod("Execute", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (execMethod != null)
                {
                    try
                    {
                        execMethod.Invoke(mod, new object[] { context });
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[SkillDefinition] Execution module invoke failed: {ex.Message}");
                    }
                }
            }
            else
            {
                if (context == null || Effects == null) return;

                // 对所有目标应用所有效果，每次都生成新实例
                foreach (var target in context.Targets)
                {
                    if (target == null) continue;
                    foreach (var effectSO in Effects)
                    {
                        var effect = effectSO?.CreateEffect();
                        if (effect != null)
                            context.ApplyStatusEffect(target, effect);
                    }
                }
            }
        }
    }
}
