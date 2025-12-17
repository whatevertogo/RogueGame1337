
using System.Collections.Generic;
using UnityEngine;
using CardSystem.SkillSystem;
using Character.Components.Interface;

namespace Character.Components
{
    /// <summary>
    /// 角色技能组件：托管多个 SkillDefinition 的槽位，负责冷却与触发
    /// TODO-实现怪物的技能
    /// </summary>
    public class EnemySkillComponent : MonoBehaviour, ISkillComponent
    {
        [Header("怪物拥有的技能")]
        public List<SkillSlot> skillSlots = new();

        protected StatusEffectComponent statusEffects;

        protected virtual void Awake()
        {
            statusEffects = GetComponent<StatusEffectComponent>();
        }

        private System.Collections.IEnumerator DelayedExecute(SkillDefinition def, Vector3? aimPoint)
        {
            yield return new WaitForSeconds(def.detectionDelay);

            var execCtx = new SkillContext(transform);

            // 两阶段行为：优先尝试 module（如果配置了 targetingModuleSO），否则回退到 legacy 枚举逻辑
            bool handledByModule = false;
            if (def != null && def.targetingModuleSO != null)
            {
                var mod = def.targetingModuleSO;
                var modType = mod.GetType();

                // 尝试通过反射调用 AcquireTargets(ctx, aimPoint)
                var acquireMethod = modType.GetMethod("AcquireTargets", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (acquireMethod != null)
                {
                    try
                    {
                        acquireMethod.Invoke(mod, new object[] { execCtx, aimPoint });
                        handledByModule = true;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[EnemySkillComponent] AcquireTargets invoke failed: {ex.Message}");
                    }
                }
                // 注意：如果模块仅实现 ManualSelectionCoroutine（交互式），敌人作为 AI 无法执行交互，这里只尝试 AcquireTargets；没有实现时回退到 legacy。
            }

            if (!handledByModule)
            {

                execCtx.Position = centre;
                var pred = CardSystem.SkillSystem.Targeting.TargetingHelper.BuildTeamPredicate(execCtx.OwnerTeam, def.targetTeam, gameObject, false);

                execCtx.Position = centre;
                var pred2 = CardSystem.SkillSystem.Targeting.TargetingHelper.BuildTeamPredicate(execCtx.OwnerTeam, def.targetTeam, gameObject, true);

            }


            // 最终执行（模块化执行器或 legacy Effect）
            def.Execute(execCtx, aimPoint);
            yield break;
        }

        public virtual bool CanUseSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= skillSlots.Count) return false;
            var slot = skillSlots[slotIndex];
            if (slot.skill == null) return false;

            // 被控制（眩晕/沉默）时无法使用
            if (statusEffects != null && (statusEffects.IsStunned || statusEffects.IsSilenced)) return false;

            // 冷却检查（使用技能自身定义的 cooldown，考虑怪物的属性冷却减速）
            var baseCd = slot.skill != null ? slot.skill.cooldown : 0f;
            var stats = GetComponent<CharacterStats>();
            var reduction = stats != null ? stats.SkillCooldownReductionRate.Value : 0f;
            var effectiveCd = Mathf.Max(0f, baseCd * (1f - reduction));
            if (Time.time < slot.lastUseTime + effectiveCd) return false;

            return true;
        }

        /// <summary>
        /// 使用技能。
        /// 可传入 explicitTarget 指定目标（单体技能），否则默认把自己作为目标（或由 SkillDefinition 负责查找目标）。
        /// </summary>
        public virtual void UseSkill(int slotIndex, Vector3? aimPoint = null)
        {
            if (!CanUseSkill(slotIndex)) return;

            var slot = skillSlots[slotIndex];
            if (slot == null || slot.skill == null) return;


            ctx.Position = centre;
            var pred = CardSystem.SkillSystem.Targeting.TargetingHelper.BuildTeamPredicate(ctx.OwnerTeam, slot.skill.targetTeam, gameObject, false);

            var centre = transform.position;
            ctx.Position = centre;
            var pred2 = CardSystem.SkillSystem.Targeting.TargetingHelper.BuildTeamPredicate(ctx.OwnerTeam, slot.skill.targetTeam, gameObject, true);

            ctx.Targets.Add(gameObject);


            // 标记冷却并触发（立即开始冷却）
            slot.lastUseTime = Time.time;

            if (slot.skill.detectionDelay <= 0f)
            {
                slot.skill.Execute(ctx, aimPoint);
            }
            else
            {
                //TODO-播放预置特效
                if (slot.skill.vfxPrefab != null)
                {
                    var tv = Instantiate(slot.skill.vfxPrefab, ctx.Position, Quaternion.identity);
                    Destroy(tv, slot.skill.detectionDelay + 0.5f);
                }
                StartCoroutine(DelayedExecute(slot.skill, aimPoint));
            }
        }
    }
}
