
using System.Collections.Generic;
using UnityEngine;
using CardSystem.SkillSystem;
using Character.Components.Interface;
using CardSystem.SkillSystem.Runtime;

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

           
            yield break;
        }

        public virtual bool CanUseSkill(int slotIndex)
        {

            return true;
        }

        /// <summary>
        /// 使用技能。
        /// 可传入 explicitTarget 指定目标（单体技能），否则默认把自己作为目标（或由 SkillDefinition 负责查找目标）。
        /// </summary>
        public virtual void UseSkill(int slotIndex, Vector3? aimPoint = null)
        {
            if (!CanUseSkill(slotIndex)) return;

           
            
        }
    }
}
