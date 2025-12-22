using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RogueGame.Events;
using CDTU.Utils;
using Character.Player;
using Character.Components;

namespace RogueGame.SaveSystem
{
    /// <summary>
    /// 存档系统工具：负责将存档数据恢复到运行时状态
    /// </summary>
    public static class SaveRestoreUtility
    {
        /// <summary>
        /// 将 RunSaveData 应用到运行时状态
        /// </summary>
        public static void RestoreRunSave(RunSaveData data)
        {
            if (data == null)
            {
                Debug.LogError("[SaveRestoreUtility] RunSaveData is null");
                return;
            }

            // 1. 恢复 InventoryManager 状态
            RestoreInventory(data);

            // 2. 恢复玩家状态
            RestorePlayerState(data);

            // 3. 恢复 GameStateManager 状态（层级、房间）
            RestoreGameState(data);

            CDTU.Utils.Logger.Log("[SaveRestoreUtility] Run 存档已恢复");
        }

        /// <summary>
        /// 恢复背包数据
        /// </summary>
        private static void RestoreInventory(RunSaveData data)
        {
            var inv = InventoryManager.Instance;
            if (inv == null)
            {
                Debug.LogWarning("[SaveRestoreUtility] InventoryManager not found");
                return;
            }

            // 设置金币
            inv.SetCoins(data.Coins);

            // 清空现有卡牌池（通过事件通知）
            EventBus.Publish(new ClearAllSlotsRequestedEvent());

            // 恢复主动卡牌
            foreach (var cardData in data.ActiveCards)
            {
                var instanceId = inv.AddActiveCardInstance(cardData.CardId, cardData.CurrentCharges);
                var state = inv.GetActiveCardState(instanceId);
                if (state != null)
                {
                    state.IsEquipped = cardData.IsEquipped;
                    state.EquippedPlayerId = cardData.EquippedPlayerId;
                }
            }

            // 恢复被动卡牌
            foreach (var cardData in data.PassiveCards)
            {
                inv.AddPassiveCard(cardData.CardId, cardData.Count);
            }

            // 恢复装备状态（通过事件通知 PlayerSkillComponent）
            var pm = PlayerManager.Instance;
            var localPlayer = pm?.GetLocalPlayerState();
            if (localPlayer != null)
            {
                foreach (var equipped in data.EquippedCards)
                {
                    EventBus.Publish(new OnPlayerSkillEquippedEvent
                    {
                        PlayerId = localPlayer.PlayerId,
                        SlotIndex = equipped.SlotIndex,
                        NewCardId = equipped.CardId
                    });
                }
            }
        }

        /// <summary>
        /// 恢复玩家状态
        /// </summary>
        private static void RestorePlayerState(RunSaveData data)
        {
            var pm = PlayerManager.Instance;
            var localPlayer = pm?.GetLocalPlayerState();
            if (localPlayer?.Controller == null)
            {
                Debug.LogWarning("[SaveRestoreUtility] PlayerController not found");
                return;
            }

            var stats = localPlayer.Controller.GetComponent<CharacterStats>();
            if (stats != null && data.PlayerData != null)
            {
                // 恢复 HP
                stats.CurrentHP = data.PlayerData.CurrentHP;

                // 恢复属性修饰符（如果需要）
                // 注意：这里只恢复基础值，可根据实际需求调整
                // stats.MaxHP.BaseValue = data.PlayerData.MaxHP;
                // ... 其他属性
            }
        }

        /// <summary>
        /// 恢复游戏状态（层级、房间）
        /// </summary>
        private static void RestoreGameState(RunSaveData data)
        {
            var gsm = GameRoot.Instance?.GameStateManager;
            if (gsm == null)
            {
                Debug.LogWarning("[SaveRestoreUtility] GameStateManager not found");
                return;
            }

            // 注意：这里需要根据你的 GameStateManager 实际 API 调整
            // 可能需要通过 RoomManager 重新加载指定的层级和房间
            CDTU.Utils.Logger.Log($"[SaveRestoreUtility] 需要恢复到 Layer {data.CurrentLayer}, Room {data.CurrentRoomId}");
            // TODO: 实现房间恢复逻辑（可能需要与 RoomManager 配合）
        }

        /// <summary>
        /// 应用 MetaSaveData 到游戏（解锁内容等）
        /// </summary>
        public static void ApplyMetaSave(MetaSaveData data)
        {
            if (data == null)
            {
                Debug.LogError("[SaveRestoreUtility] MetaSaveData is null");
                return;
            }

            // TODO: 根据实际需求实现解锁系统
            // 例如：通知 UI 更新解锁的卡牌、角色等
            CDTU.Utils.Logger.Log($"[SaveRestoreUtility] Meta 存档已加载：已解锁 {data.UnlockedCards.Count} 张卡牌");
        }
    }
}
