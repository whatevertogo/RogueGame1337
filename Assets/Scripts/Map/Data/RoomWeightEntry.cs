using UnityEngine;

namespace RogueGame.Map.Data
{
    /// <summary>
    /// 单个房间类型的权重条目（基于当前房间类型的转移）。
    /// </summary>
    [System.Serializable]
    public class RoomWeightEntry
    {
        public RogueGame.Map.RoomType NextType;
        public float Weight = 1f;
    }
}
