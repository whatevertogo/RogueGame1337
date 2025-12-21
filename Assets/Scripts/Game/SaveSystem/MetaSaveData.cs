using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueGame.SaveSystem
{
    /// <summary>
    /// 元游戏存档数据 - 跨 Run 持久化的数据（解锁、统计等）
    /// </summary>
    [Serializable]
    public class MetaSaveData
    {
        [Header("基础信息")]
        public string PlayerName = "Player";
        public long LastSaveTime;
        
        [Header("解锁内容")]
        public List<string> UnlockedCards = new();  // 已解锁的卡牌 ID
        public List<string> UnlockedCharacters = new();  // 已解锁的角色 ID
        
        [Header("游戏统计")]
        public int TotalRuns = 0;  // 总游戏次数
        public int SuccessfulRuns = 0;  // 通关次数
        public int TotalKills = 0;  // 总击杀数
        public int TotalDeaths = 0;  // 总死亡次数
        public long TotalDamageDealt = 0;  // 总造成伤害
        public float TotalPlayTime = 0f;  // 总游戏时长（秒）
        
        [Header("最佳记录")]
        public int HighestLayerReached = 0;  // 到达的最高层级
        public int MostKillsInOneRun = 0;  // 单次 Run 最多击杀
        public long MostDamageInOneRun = 0;  // 单次 Run 最高伤害
        
        public MetaSaveData()
        {
            LastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        
        /// <summary>
        /// 获取友好的存档时间显示
        /// </summary>
        public string GetFormattedSaveTime()
        {
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(LastSaveTime).ToLocalTime();
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
