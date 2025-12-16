
using System.Collections.Generic;
using UnityEngine;
using CardSystem.SkillSystem;

namespace Character.Components
{
    [System.Serializable]
    public class SkillSlot
    {
        public SkillDefinition skill;
        public float cooldown = 1f;
        [HideInInspector] public float lastUseTime = -Mathf.Infinity;
    }

    /// <summary>
    /// 角色技能组件：托管多个 SkillDefinition 的槽位，负责冷却与触发
    /// </summary>
    public class CharacterSkillComponent : MonoBehaviour
    {
        [Header("技能槽")]
        public List<SkillSlot> skillSlots = new();

        protected StatusEffectComponent statusEffects;

        protected virtual void Awake()
        {
            statusEffects = GetComponent<StatusEffectComponent>();
        }

        public virtual bool CanUseSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= skillSlots.Count) return false;
            var slot = skillSlots[slotIndex];
            if (slot.skill == null) return false;

            // 被控制（眩晕/沉默）时无法使用
            if (statusEffects != null && (statusEffects.IsStunned || statusEffects.IsSilenced)) return false;

            // 冷却检查
            if (Time.time < slot.lastUseTime + slot.cooldown) return false;

            return true;
        }

        /// <summary>
        /// 使用技能。
        /// 可传入 explicitTarget 指定目标（单体技能），否则默认把自己作为目标（或由 SkillDefinition 负责查找目标）。
        /// </summary>
        public virtual void UseSkill(int slotIndex, GameObject explicitTarget = null)
        {
            if (!CanUseSkill(slotIndex)) return;

            var slot = skillSlots[slotIndex];
            if (slot == null || slot.skill == null) return;

            var ctx = new SkillContext(transform);

            if (explicitTarget != null)
            {
                ctx.Targets.Add(explicitTarget);
            }
            else
            {
                // 默认把自己放进 Targets，SkillDefinition 可决定如何使用
                ctx.Targets.Add(gameObject);
            }

            slot.skill.Execute(ctx);

            slot.lastUseTime = Time.time;
        }
    }
}