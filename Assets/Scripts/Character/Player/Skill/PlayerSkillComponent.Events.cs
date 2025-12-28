using System;
using UnityEngine;
using Character.Components;
using Character.Components.Interface;
using RogueGame.Events;

namespace Character.Player
{
    /// <summary>
    /// PlayerSkillComponent - 事件处理部分
    /// </summary>
    public partial class PlayerSkillComponent : MonoBehaviour, ISkillComponent
    {
        #region 事件

        // UI 事件（保留兼容性）
        public event Action<int, float> OnEnergyChanged;
        public event Action<int> OnSkillUsed;
        public event Action<int, string> OnSkillEquipped;
        public event Action<int> OnSkillUnequipped;

        #endregion

        #region 事件订阅处理

        /// <summary>
        /// 初始化事件订阅
        /// </summary>
        private void SubscribeEvents()
        {
            // 订阅清空槽位事件
            EventBus.Subscribe<ClearAllSlotsRequestedEvent>(evt =>
            {
                // 清理所有槽位
                for (int i = 0; i < _playerSkillSlots.Length; i++)
                {
                    UnequipActiveCardBySlotIndex(i);
                }
            });

            // 订阅技能装备变化事件
            EventBus.Subscribe<OnPlayerSkillEquippedEvent>(OnPlayerSlotCardChanged);

            // 订阅能量变化事件，自动更新 UI
            EventBus.Subscribe<ActiveCardChargesChangedEvent>(OnEnergyChangedFromInventory);
        }

        /// <summary>
        /// 取消事件订阅
        /// </summary>
        private void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<OnPlayerSkillEquippedEvent>(OnPlayerSlotCardChanged);
            EventBus.Unsubscribe<ActiveCardChargesChangedEvent>(OnEnergyChangedFromInventory);
        }

        /// <summary>
        /// 处理玩家技能装备变化事件
        /// </summary>
        private void OnPlayerSlotCardChanged(OnPlayerSkillEquippedEvent @event)
        {
            // 获取拥有此组件的 PlayerController
            var pc = GetComponent<PlayerController>();
            if (pc == null) return;

            // 获取该 Controller 对应的运行时状态
            var pr = PlayerManager.Instance?.GetPlayerRuntimeStateByController(pc);
            // 如果找不到或者事件的 PlayerId 不匹配，则忽略
            if (pr == null || @event.PlayerId != pr.PlayerId) return;

            int slotIndex = @event.SlotIndex;
            string newCardId = @event.NewCardId;

            if (string.IsNullOrEmpty(newCardId))
            {
                // 取消装备
                UnequipActiveCardBySlotIndex(slotIndex);
            }
            else
            {
                // 装备新卡
                EquipActiveCardToSlotIndex(slotIndex, newCardId);
            }
        }

        #endregion
    }
}
