
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
            if (def.targetingMode == CardSystem.SkillSystem.Enum.SkillTargetingMode.AOE)
            {
                var centre = aimPoint.HasValue ? aimPoint.Value : transform.position;
                execCtx.Position = centre;
                CardSystem.SkillSystem.Targeting.TargetingHelper.GetAoeTargets(centre, def.radius, def.targetMask, execCtx.Targets,
                    go => CardSystem.SkillSystem.Targeting.TargetingHelper.IsHostileTo(execCtx.OwnerTeam, go));
            }
            else if (def.targetingMode == CardSystem.SkillSystem.Enum.SkillTargetingMode.SelfTarget)
            {
                var centre = transform.position;
                execCtx.Position = centre;
                CardSystem.SkillSystem.Targeting.TargetingHelper.GetAoeTargets(centre, def.radius, def.targetMask, execCtx.Targets,
                    go => go != gameObject);
            }

            def.Execute(execCtx);
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

            var ctx = new SkillContext(this.transform);

            if (slot.skill.targetingMode == CardSystem.SkillSystem.Enum.SkillTargetingMode.AOE)
            {
                var centre = aimPoint.HasValue ? aimPoint.Value : transform.position;
                ctx.Position = centre;
                CardSystem.SkillSystem.Targeting.TargetingHelper.GetAoeTargets(centre, slot.skill.radius, slot.skill.targetMask, ctx.Targets,
                    go => CardSystem.SkillSystem.Targeting.TargetingHelper.IsHostileTo(ctx.OwnerTeam, go));
            }
            else if (slot.skill.targetingMode == CardSystem.SkillSystem.Enum.SkillTargetingMode.SelfTarget)
            {
                // SelfTarget: 以自身为中心的 AOE，但排除自身（用于治疗/增益盟友）
                var centre = transform.position;
                ctx.Position = centre;
                CardSystem.SkillSystem.Targeting.TargetingHelper.GetAoeTargets(centre, slot.skill.radius, slot.skill.targetMask, ctx.Targets,
                    go => go != gameObject);
            }
            else if(slot.skill.targetingMode == CardSystem.SkillSystem.Enum.SkillTargetingMode.Self)
            {
                // 默认把自己放进 Targets
                ctx.Targets.Add(gameObject);
            }

            // 标记冷却并触发（立即开始冷却）
            slot.lastUseTime = Time.time;

            if (slot.skill.detectionDelay <= 0f)
            {
                slot.skill.Execute(ctx);
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