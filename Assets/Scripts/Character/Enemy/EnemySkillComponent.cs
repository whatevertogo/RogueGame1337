
using System.Collections.Generic;
using UnityEngine;
using CardSystem.SkillSystem;
using Character.Components.Interface;
using CardSystem.SkillSystem.Runtime;

namespace Character.Components
{
    /// <summary>
    /// 敌人技能组件：托管多个 SkillDefinition 的槽位，负责冷却与触发
    /// TODO: 实现怪物的技能逻辑
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

        public virtual bool CanUseSkill(int slotIndex)
        {
            // TODO: 实现冷却检查
            if (slotIndex < 0 || slotIndex >= skillSlots.Count) return false;
            var slot = skillSlots[slotIndex];
            if (slot == null || slot.Runtime == null || slot.Runtime.Skill == null) return false;
            
            return true;
        }

        /// <summary>
        /// 使用技能。
        /// 可传入 aimPoint 指定目标点，否则默认使用怪物位置
        /// </summary>
        public virtual void UseSkill(int slotIndex, Vector3? aimPoint = null)
        {
            if (!CanUseSkill(slotIndex)) return;

            var slot = skillSlots[slotIndex];
            var rt = slot?.Runtime;
            if (rt == null || rt.Skill == null) return;

            var def = rt.Skill;

            // 构建上下文
            var ctx = new SkillContext
            {
                Caster = GetComponent<CharacterBase>(),
                Targets = null,
                AimPoint = aimPoint ?? transform.position,
                SlotIndex = slotIndex,
                CardId = rt.CardId
            };

            // 执行技能
            if (def.executor != null)
            {
                def.executor.Execute(def, ctx);
            }
            else
            {
                Debug.LogWarning($"[EnemySkillComponent] Skill '{def.skillId}' has no executor configured.");
            }

            // 记录使用时间（用于冷却）
            rt.LastUseTime = Time.time;
        }
    }
}
