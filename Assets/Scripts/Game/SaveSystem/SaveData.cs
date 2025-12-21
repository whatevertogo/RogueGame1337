using System;
using System.Collections.Generic;
using UnityEngine;
using RogueGame.Map;

namespace RogueGame.SaveSystem
{
    /// <summary>
    /// 单局存档数据结构 - 用于保存/加载一次 Run 的进度
    /// </summary>
    [Serializable]
    public class RunSaveData
    {
        [Header("基础信息")]
        public string PlayerName = "Player";
        public int CurrentLayer = 1;
        public int CurrentRoomId = 0;
        public long SaveTime;
        
        [Header("玩家状态")]
        public PlayerSaveData PlayerData;
        
        [Header("背包和卡牌")]
        public int Coins = 0;
        public List<ActiveCardSaveData> ActiveCards = new();  // 主动卡牌（包含充能）
        public List<PassiveCardSaveData> PassiveCards = new();  // 被动卡牌（包含层数）
        public List<EquippedCardData> EquippedCards = new();
        
        [Header("房间进度")]
        public List<RoomProgressData> CompletedRooms = new();
        
        [Header("本局统计")]
        public int KillsThisRun = 0;
        public int DamageDealtThisRun = 0;
        
        public RunSaveData()
        {
            SaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        
        /// <summary>
        /// 获取友好的存档时间显示
        /// </summary>
        public string GetFormattedSaveTime()
        {
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(SaveTime).ToLocalTime();
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
    
    [Serializable]
    public class PlayerSaveData
    {
        public float CurrentHP;
        public float MaxHP;
        
        // 基础属性（与 CharacterStats 对齐）
        public float AttackPower;
        public float AttackSpeed;
        public float MoveSpeed;
        public float Armor;
        public float Dodge;
        public float SkillCooldownReductionRate;
    }
    
    /// <summary>
    /// 主动卡牌存档数据（对齐 InventoryManager.ActiveCardState）
    /// </summary>
    [Serializable]
    public class ActiveCardSaveData
    {
        public string CardId;
        public string InstanceId;  // 实例 ID
        public int CurrentCharges;  // 当前充能
        public bool IsEquipped;
        public string EquippedPlayerId;
    }
    
    /// <summary>
    /// 被动卡牌存档数据
    /// </summary>
    [Serializable]
    public class PassiveCardSaveData
    {
        public string CardId;
        public int Count;  // 叠加层数
    }
    
    [Serializable]
    public class EquippedCardData
    {
        public int SlotIndex;
        public string CardId;
        public string InstanceId;  // 对应 InventoryManager 的 instanceId
    }
    
    [Serializable]
    public class RoomProgressData
    {
        public int Layer;
        public int RoomId;
        public RoomType RoomType;
        public bool IsCompleted;
        public long CompletionTime;
    }
}