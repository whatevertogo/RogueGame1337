using Character.Components;
using Core.Events;
using Game;
using RogueGame.Events;

namespace Character.Player
{
    /// <summary>
    /// PlayerSkillComponent - 事件处理部分
    /// 职责：订阅技能相关事件，转发给 PlayerManager
    /// </summary>
    public sealed partial class PlayerSkillComponent
    {
        private string _playerId;

        /// <summary>
        /// 设置玩家 ID（由 PlayerManager 调用）
        /// </summary>
        internal void SetPlayerId(string playerId)
        {
            _playerId = playerId;
        }

        /// <summary>
        /// 启用事件转发
        /// </summary>
        internal void EnableEventForwarding()
        {
            SubscribeEvents();
        }

        /// <summary>
        /// 禁用事件转发
        /// </summary>
        internal void DisableEventForwarding()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            // 订阅库存能量变化事件
            if (GameRoot.Instance?.InventoryManager != null)
            {
                GameRoot.Instance.InventoryManager.OnActiveCardEnergyChanged += OnEnergyChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            // 取消订阅
            if (GameRoot.Instance?.InventoryManager != null)
            {
                GameRoot.Instance.InventoryManager.OnActiveCardEnergyChanged -= OnEnergyChanged;
            }
        }

        /// <summary>
        /// 能量变化处理
        /// </summary>
        private void OnEnergyChanged(string instanceId, int energy)
        {
            // 查找匹配的技能槽
            for (int i = 0; i < SlotCount; i++)
            {
                var rt = GetRuntime(i);
                if (rt?.InstanceId == instanceId)
                {
                    ForwardSkillEnergyChanged(i, energy);
                    return;
                }
            }
        }

        /// <summary>
        /// 技能使用事件（由 Executor 触发）
        /// </summary>
        internal void OnSkillUsedInternally(int slotIndex)
        {
            ForwardSkillUsed(slotIndex);
        }

        /// <summary>
        /// 技能装备事件
        /// </summary>
        internal void OnSkillEquippedInternally(int slotIndex, string cardId)
        {
            ForwardSkillEquipped(slotIndex, cardId);
        }

        /// <summary>
        /// 技能卸载事件
        /// </summary>
        internal void OnSkillUnequippedInternally(int slotIndex, string cardId)
        {
            ForwardSkillUnequipped(slotIndex, cardId);
        }

        // ========== 事件转发方法 ==========

        private void ForwardSkillEnergyChanged(int slotIndex, int energy)
        {
            if (string.IsNullOrEmpty(_playerId)) return;

            var manager = PlayerManager.Instance;
            if (manager != null)
            {
                // 计算归一化能量值
                var rt = GetRuntime(slotIndex);
                if (rt == null)
                {
                    // 当前槽位没有有效技能运行时，直接忽略本次能量变更
                    return;
                }

                var cardDatabase = GameRoot.Instance?.CardDatabase;
                if (cardDatabase == null)
                {
                    // 全局卡牌数据库不可用，无法解析技能配置，避免发送错误数据
                    return;
                }

                var cardDef = cardDatabase.Resolve(rt.CardId);
                if (cardDef == null)
                {
                    // 未能解析到对应卡牌定义，避免使用默认 maxEnergy 发送错误归一化值
                    return;
                }

                int maxEnergy = cardDef.activeCardConfig.maxEnergy;
                float normalized = (float)energy / maxEnergy;

                manager.RaisePlayerSkillEnergyChanged(_playerId, slotIndex, normalized);
            }
        }

        private void ForwardSkillUsed(int slotIndex)
        {
            if (string.IsNullOrEmpty(_playerId)) return;
            PlayerManager.Instance?.RaisePlayerSkillUsed(_playerId, slotIndex);
        }

        private void ForwardSkillEquipped(int slotIndex, string cardId)
        {
            if (string.IsNullOrEmpty(_playerId)) return;
            PlayerManager.Instance?.RaisePlayerSkillEquipped(_playerId, slotIndex, cardId);
        }

        private void ForwardSkillUnequipped(int slotIndex, string cardId)
        {
            if (string.IsNullOrEmpty(_playerId)) return;
            PlayerManager.Instance?.RaisePlayerSkillUnequipped(_playerId, slotIndex);
        }
    }
}
