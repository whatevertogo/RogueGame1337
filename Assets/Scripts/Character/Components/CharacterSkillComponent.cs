
using UnityEngine;

namespace Character.Components
{
    public class CharacterSkillComponent : MonoBehaviour
    {
        protected virtual void Awake()
        {
            // 基类初始化逻辑
        }

        public virtual bool CanUseSkill(int slotIndex)
        {
            // 基类技能使用条件
            return true;
        }

        public virtual void UseSkill(int slotIndex)
        {
            // 基类技能使用逻辑
        }
    }
}