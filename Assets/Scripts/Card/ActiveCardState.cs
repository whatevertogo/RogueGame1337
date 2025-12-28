using System;
using UnityEngine;

namespace RogueGame.Items
{
    /// <summary>
    /// 主动卡运行时状态
    /// </summary>
    [Serializable]
    public class ActiveCardState
    {
        public string CardId;            // 索引到 GameRoot.Instance.CardDataBase
        public string InstanceId;        // 可选：若卡牌不是全局唯一
        public bool IsEquipped;
        public int CurrentCharges;
        public float CooldownRemaining;
        public int Level = 1;            // 技能等级（Lv1-Lv5），用于主动技能升级系统
        public string EquippedPlayerId;  // null 表示在池中
    }
}
