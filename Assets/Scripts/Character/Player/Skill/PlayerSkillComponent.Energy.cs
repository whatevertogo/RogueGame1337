using System;
using UnityEngine;
using Character.Components;
using Character.Components.Interface;
using RogueGame.Events;

namespace Character.Player
{
    /// <summary>
    /// PlayerSkillComponent - 能量验证部分
    /// </summary>
    public partial class PlayerSkillComponent : MonoBehaviour, ISkillComponent
    {
        #region 能量验证

        /// <summary>
        /// 检查指定槽位的技能是否有足够能量
        /// </summary>
        public bool HasEnoughEnergy(int slotIndex, int threshold)
        {
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return false;
            var rt = _playerSkillSlots[slotIndex]?.Runtime;
            if (rt == null || string.IsNullOrEmpty(rt.InstanceId)) return false;

            var inv = InventoryServiceManager.Instance;
            if (inv == null) return false;

            var state = inv.GetActiveCardState(rt.InstanceId);
            if (state == null) return false;

            return state.CurrentCharges >= threshold;
        }

        /// <summary>
        /// 获取指定槽位的当前能量
        /// </summary>
        public int GetCurrentEnergy(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return 0;
            var rt = _playerSkillSlots[slotIndex]?.Runtime;
            if (rt == null || string.IsNullOrEmpty(rt.InstanceId)) return 0;

            var inv = InventoryServiceManager.Instance;
            if (inv == null) return 0;

            var state = inv.GetActiveCardState(rt.InstanceId);
            if (state == null) return 0;

            return state.CurrentCharges;
        }

        /// <summary>
        /// 获取指定槽位的最大能量
        /// </summary>
        public int GetMaxEnergy(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length) return 100;
            var rt = _playerSkillSlots[slotIndex]?.Runtime;
            if (rt == null) return 100;

            var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(rt.CardId);
            if (cardDef?.activeCardConfig == null) return 100;

            return Mathf.Max(1, cardDef.activeCardConfig.maxEnergy);
        }

        /// <summary>
        /// 获取能量比例（0-1，用于 UI 显示）
        /// </summary>
        public float GetEnergyRatio(int slotIndex)
        {
            int max = GetMaxEnergy(slotIndex);
            if (max <= 0) return 0f;
            return (float)GetCurrentEnergy(slotIndex) / max;
        }

        /// <summary>
        /// 当能量变化时，自动更新对应槽位的 UI
        /// </summary>
        private void OnEnergyChangedFromInventory(ActiveCardChargesChangedEvent evt)
        {
            // 查找使用此 instanceId 的槽位
            for (int i = 0; i < _playerSkillSlots.Length; i++)
            {
                var rt = _playerSkillSlots[i]?.Runtime;
                if (rt != null && rt.InstanceId == evt.InstanceId)
                {
                    // 获取最大能量并归一化
                    int maxEnergy = GameRoot.Instance?.CardDatabase?.Resolve(rt.CardId)?.activeCardConfig?.maxEnergy ?? 100;
                    float norm = maxEnergy > 0 ? (float)evt.NewCharges / maxEnergy : 0f;
                    OnEnergyChanged?.Invoke(i, norm);
                    break;
                }
            }
        }

        #endregion
    }
}
