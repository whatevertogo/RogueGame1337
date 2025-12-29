using UnityEngine;
using Character.Components.Interface;

namespace Character.Player
{
    /// <summary>
    /// PlayerSkillComponent - 打断控制部分
    /// </summary>
    public partial class PlayerSkillComponent : MonoBehaviour, ISkillComponent
    {
        #region 打断控制

        /// <summary>
        /// 取消指定槽位正在运行的技能协程（若有）
        /// </summary>
        /// <param name="slotIndex">技能槽位的索引</param>
        public void CancelSlotCoroutine(int slotIndex)
        {
            // 检查玩家技能槽数组是否为空
            if (_playerSkillSlots == null) return;
            // 检查索引是否有效，防止数组越界
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return;
            // 获取指定槽位的运行时信息
            var rt = _playerSkillSlots[slotIndex]?.Runtime;
            // 如果运行时信息为空，则直接返回
            if (rt == null) return;
            // 如果存在正在运行的协程，则停止它并清除引用
            if (rt.RunningCoroutine != null)
            {
                StopCoroutine(rt.RunningCoroutine);
                rt.RunningCoroutine = null;
            }
        }

        /// <summary>
        /// 取消所有槽位的技能协程（用于死亡/禁用时）
        /// </summary>
        public void CancelAllSkillCoroutines()
        {
            if (_playerSkillSlots == null) return;
            for (int i = 0; i < _playerSkillSlots.Length; i++)
            {
                CancelSlotCoroutine(i);
            }
        }

        /// <summary>
        /// 打断技能（用于控制效果如眩晕、击飞等）
        /// </summary>
        /// <param name="slotIndex">技能槽位索引，-1 表示打断所有正在施放的技能</param>
        /// <param name="refundCharges">是否退还充能</param>
        public void InterruptSkill(int slotIndex = -1, bool refundCharges = false)
        {
            if (slotIndex < 0)
            {
                // 打断所有技能
                for (int i = 0; i < _playerSkillSlots.Length; i++)
                {
                    InterruptSingleSkill(i, refundCharges);
                }
            }
            else
            {
                InterruptSingleSkill(slotIndex, refundCharges);
            }
        }

        /// <summary>
        /// 打断单个技能槽位
        /// </summary>
        /// <param name="slotIndex">技能槽位索引</param>
        /// <param name="refundCharges">是否退还充能</param>
        private void InterruptSingleSkill(int slotIndex, bool refundCharges)
        {
            // 检查槽位索引是否有效，无效则直接返回
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return;
            // 获取技能槽位对应的运行时对象，如果为空则直接返回
            var rt = _playerSkillSlots[slotIndex]?.Runtime;
            if (rt == null) return;

            // 取消协程
            if (rt.RunningCoroutine != null)
            {
                StopCoroutine(rt.RunningCoroutine);
                rt.RunningCoroutine = null;
            }

            // 如果需要退还充能（使用缓存配置）
            // 增加 _inventory 非空检查，避免在初始化失败时抛出 NullReferenceException
            if (refundCharges && !string.IsNullOrEmpty(rt.InstanceId) && _inventory != null)
            {
                var config = rt.CachedActiveConfig;
                if (config?.requiresCharge == true)
                {
                    _inventory.AddEnergy(rt.InstanceId, config.energyThreshold);
                }
            }
        }

        #endregion
    }
}
