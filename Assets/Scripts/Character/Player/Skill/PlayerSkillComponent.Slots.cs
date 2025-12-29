using System;
using UnityEngine;
using Character.Components;
using Character.Components.Interface;
using RogueGame.Events;
using Character.Player.Skill.Runtime;

namespace Character.Player
{
    /// <summary>
    /// PlayerSkillComponent - 槽位管理部分
    /// </summary>
    public partial class PlayerSkillComponent : MonoBehaviour, ISkillComponent
    {
        #region 槽位管理

        /// <summary>
        /// 获取指定槽位
        /// </summary>
        public SkillSlot GetSlot(int index)
        {
            if (index < 0 || index >= _playerSkillSlots.Length)
                return null;
            return _playerSkillSlots[index];
        }

        /// <summary>
        /// 获取指定槽位的运行时状态
        /// </summary>
        public ActiveSkillRuntime GetRuntime(int index)
        {
            if (index < 0 || index >= _playerSkillSlots.Length)
                return null;
            return _playerSkillSlots[index]?.Runtime;
        }

        /// <summary>
        /// 检查槽位是否为空
        /// </summary>
        public bool IsSlotEmpty(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _playerSkillSlots.Length)
                return true;
            return _playerSkillSlots[slotIndex] == null ||
                   _playerSkillSlots[slotIndex].Runtime == null;
        }

        /// <summary>
        /// 装备主动卡到指定槽位
        /// </summary>
        public void EquipActiveCardToSlotIndex(int slotIndex, string cardId)
        {
            // 不查 Inventory
            var cardDef = GameRoot.Instance?.CardDatabase?.Resolve(cardId);
            if (cardDef == null)
            {
                CDTU.Utils.CDLogger.LogWarning($"[PlayerSkillComponent] Equip failed: cardDef for id '{cardId}' is null. slotIndex={slotIndex}");
                return;
            }

            var skillDef = cardDef.activeCardConfig.skill;
            if (_playerSkillSlots[slotIndex] == null)
                _playerSkillSlots[slotIndex] = new SkillSlot();

            // 获取或创建 Inventory 中的 ActiveCardState 实例，并把实例 id 关联到 runtime
            var inv = InventoryServiceManager.Instance;
            string instanceId = null;
            if (inv != null)
            {
                var existing = inv.GetFirstInstanceByCardId(cardId);
                if (existing != null)
                {
                    instanceId = existing.InstanceId;
                }
                else
                {
                    // 使用内部方法创建实例（不触发升级/去重逻辑）
                    instanceId = inv.CreateActiveCardInstanceInternal(cardId, 0);
                }

                // 尝试把实例标记为被此玩家装备（若能找到 playerId）
                var pc = GetComponent<PlayerController>();
                if (pc != null)
                {
                    var pr = PlayerManager.Instance?.GetPlayerRuntimeStateByController(pc);
                    if (pr != null)
                    {
                        inv.MarkInstanceEquipped(instanceId, pr.PlayerId);
                    }
                }
            }

            _playerSkillSlots[slotIndex].Equip(new ActiveSkillRuntime(cardId, skillDef, instanceId));

            // 广播初始能量/充能状态
            if (cardDef.activeCardConfig != null && cardDef.activeCardConfig.requiresCharge)
            {
                int max = Mathf.Max(1, cardDef.activeCardConfig.maxEnergy);
                int current = max; // default

                if (inv != null && !string.IsNullOrEmpty(instanceId))
                {
                    var state = inv.GetActiveCardState(instanceId);
                    if (state != null) current = state.CurrentCharges;
                }

                float norm = (float)current / max;
                OnEnergyChanged?.Invoke(slotIndex, norm);
            }
            else
            {
                // 非充能技能，默认满能量（可用）
                OnEnergyChanged?.Invoke(slotIndex, 1f);
            }

            OnSkillEquipped?.Invoke(slotIndex, cardId);
        }

        /// <summary>
        /// 卸载指定槽位的主动卡
        /// </summary>
        public void UnequipActiveCardBySlotIndex(int slotIndex)
        {
            var slot = _playerSkillSlots[slotIndex];
            if (slot == null) return;
            var instanceId = slot.Runtime?.InstanceId;
            // 取消装备标记
            if (!string.IsNullOrEmpty(instanceId))
            {
                InventoryServiceManager.Instance?.MarkInstanceEquipped(instanceId, null);
            }
            slot.Clear();
            OnSkillUnequipped?.Invoke(slotIndex);
        }

        /// <summary>
        /// 清空所有槽位
        /// </summary>
        public void ClearAllSlots()
        {
            for (int i = 0; i < _playerSkillSlots.Length; i++)
            {
                UnequipActiveCardBySlotIndex(i);
            }
        }


        #endregion
    }
}
