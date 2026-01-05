using System;
using UnityEngine;

namespace RogueGame.Items
{
    /// <summary>
    /// 主动卡运行时状态（纯充能系统，无CD）
    /// 重构：移除进化历史，由 Runtime 管理，但保留等级用于持久化
    /// </summary>
    [Serializable]
    public class ActiveCardState
    {
        public string CardId;            // 索引到 GameRoot.Instance.CardDataBase
        public string InstanceId;        // 可选：若卡牌不是全局唯一
        public bool IsEquipped;
        public int CurrentEnergy;
        public string EquippedPlayerId;  // null 表示在池中
        
        /// <summary>
        /// 当前技能等级（1-5），用于持久化和 UI 显示
        /// </summary>
        public int Level = 1;
    }
}
