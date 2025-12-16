using UnityEngine;

namespace Character.Components.Interface
{
    /// <summary>
    /// 角色技能组件接口
    /// </summary>
    public interface ISkillComponent
    {
        bool CanUseSkill(int slotIndex);
        void UseSkill(int slotIndex, Vector3? aimPoint = null);
    }
}