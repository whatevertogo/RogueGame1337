using System;
using UnityEngine;
using Character.Components;
using Character.Components.Interface;
using Character.Player.Skill.Runtime;

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
        public void CancelSlotCoroutine(int slotIndex)
        {
            if (_playerSkillSlots == null) return;
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return;
            var rt = _playerSkillSlots[slotIndex]?.Runtime;
            if (rt == null) return;
            if (rt.RunningCoroutine != null)
            {
                try
                {
                    StopCoroutine(rt.RunningCoroutine);
                }
                catch { }
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
        private void InterruptSingleSkill(int slotIndex, bool refundCharges)
        {
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return;
            var rt = _playerSkillSlots[slotIndex]?.Runtime;
            if (rt == null) return;

            // 取消协程
            if (rt.RunningCoroutine != null)
            {
                try
                {
                    StopCoroutine(rt.RunningCoroutine);
                }
                catch { }
                rt.RunningCoroutine = null;
            }

            // 如果需要退还充能
            if (refundCharges && !string.IsNullOrEmpty(rt.InstanceId))
            {
                var inv = InventoryServiceManager.Instance;
                var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(rt.CardId);
                if (inv != null && cardDef != null && cardDef.activeCardConfig != null && cardDef.activeCardConfig.requiresCharge)
                {
                    // 退还能量阈值（用于打断技能时返还消耗的能量）
                    int refundAmount = cardDef.activeCardConfig.energyThreshold;
                    inv.AddEnergy(rt.InstanceId, refundAmount);
                }
            }

            // 重置使用标记（允许再次使用）
            rt.UsedInCurrentRoom = false;
        }

        #endregion
    }
}
