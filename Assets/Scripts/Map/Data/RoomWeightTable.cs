using System.Collections.Generic;
using UnityEngine;

namespace RogueGame.Map.Data
{
    /// <summary>
    /// 基于“当前房间类型”到“下一房间类型”的权重表。
    /// </summary>
    [CreateAssetMenu(fileName = "RoomWeightTable", menuName = "RogueGame/Map/Room Weight Table")]
    public class RoomWeightTable : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public RoomType CurrentType;
            public List<RoomWeightEntry> Candidates = new();
        }

        public int BossAfterRooms = 6;
        public List<Entry> Entries = new();

        public List<RoomWeightEntry> GetWeights(RoomType current)
        {
            var found = Entries.Find(e => e.CurrentType == current);
            return found != null ? found.Candidates : null;
        }
    }
}
