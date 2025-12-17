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

        // 已移除旧的 legacy targeting 字段（请改用 `targetingModuleSO` 来定义目标选择策略）
        public GameObject vfxPrefab;  // 可选

        [Header("效果列表 (legacy)")]
        [InlineEditor]
        public List<SkillEffectSO> Effects;

        [Header("Modular (preferred)")]
        [Tooltip("可选：目标选择模块（作为 ScriptableObject 引用）。若配置为 TargetingModuleSO 的实例，运行时会通过反射调用 AcquireTargets/ManualSelectionCoroutine")]
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

            // 模块化目标采集（运行时反射）
            // - 若配置了 targetingModuleSO 且模块为非交互式（RequiresManualSelection == false），则通过反射调用 AcquireTargets(context, aimPoint)
            // - 若模块是交互式的（RequiresManualSelection == true），则应由调用方（如 PlayerSkillComponent）在交互完成后再调用 Execute（交互协程负责填充 context.ExplicitTarget / context.Targets）
            if (targetingModuleSO != null)
            {
                var mod = targetingModuleSO;
                var modType = mod.GetType();

                bool requiresManual = false;
                var requiresProp = modType.GetProperty("RequiresManualSelection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (requiresProp != null && requiresProp.PropertyType == typeof(bool))
                {
                    var v = requiresProp.GetValue(mod);
                    if (v is bool b) requiresManual = b;
                }

                if (!requiresManual)
                {
                    var acquireMethod = modType.GetMethod("AcquireTargets", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (acquireMethod != null)
                    {
                        try
                        {
                            acquireMethod.Invoke(mod, new object[] { context, aimPoint });
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[SkillDefinition] AcquireTargets 调用失败：{ex.Message}");
                        }
                    }
                }
                // 若 requiresManual == true，则交互式选择应在调用方处理（PlayerSkillComponent）
            }
            else
            {
                // 未配置目标模块时记录警告，目标留空（Execution 模块负责如何处理空 Targets）
                Debug.LogWarning($"[SkillDefinition] 技能 '{skillId}' 未配置 targetingModuleSO，建议迁移到模块化目标选择。");
            }

            // 如果 Targets 为空但 explicit target 存在，则把显式目标作为单体目标
            if ((context.Targets == null || context.Targets.Count == 0) && context.ExplicitTarget != null)
            {
                context.Targets.Add(context.ExplicitTarget);
            }

            // 执行阶段：优先使用 executionModuleSO（通过反射调用 Execute），若未配置则回退到 Effects（仅对状态效果）
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
                        Debug.LogWarning($"[SkillDefinition] Execute 调用失败：{ex.Message}");
                    }
                }
            }
            else
            {
                if (Effects == null) return;

                // 对所有目标应用状态效果（保持原有 Effects 的兼容行为）
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
