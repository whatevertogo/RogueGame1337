using System.Collections.Generic;
using RogueGame.Map.Data;

namespace RogueGame.Map
{
    /// <summary>
    /// 按权重与 Boss 阈值选择下一房间类型与索引。
    /// </summary>
    public sealed class RoomSelector
    {
        private readonly RoomWeightTable _weightTable;
        private readonly Dictionary<RoomType, RoomVariantSet> _variantSets;
        private readonly System.Random _rng;

        public int StepCount { get; private set; }

        public RoomSelector(RoomWeightTable weightTable, Dictionary<RoomType, RoomVariantSet> variantSets, int seed)
        {
            _weightTable = weightTable;
            _variantSets = variantSets;
            _rng = new System.Random(seed);
            StepCount = 0;
        }

        /// <summary>
        /// 选择下一房间元数据（根据权重）。
        /// </summary>
        public RoomMeta NextRoom(RoomType currentType)
        {
            // Boss 阈值
            if (_weightTable != null && StepCount + 1 >= _weightTable.BossAfterRooms)
            {
                return BuildMeta(RoomType.Boss, PickIndex(RoomType.Boss));
            }

            // 权重选择
            var list = _weightTable?.GetWeights(currentType);
            if (list == null || list.Count == 0)
            {
                // fallback: Normal
                return BuildMeta(RoomType.Normal, PickIndex(RoomType.Normal));
            }

            // 直接从权重列表中选择（不过滤）
            var filtered = new List<Data.RoomWeightEntry>();
            foreach (var e in list)
            {
                if (e.Weight > 0) filtered.Add(e);
            }
            if (filtered.Count == 0) filtered.AddRange(list);

            var pickedType = WeightedPick(filtered);
            return BuildMeta(pickedType, PickIndex(pickedType));
        }

        /// <summary>
        /// 按权重选择房间类型。
        /// </summary>
        private RoomType WeightedPick(List<Data.RoomWeightEntry> entries)
        {
            float total = 0;
            foreach (var e in entries)
                total += UnityEngine.Mathf.Max(0, e.Weight);
            if (total <= 0)
                return entries[0].NextType;
            float roll = (float)(_rng.NextDouble() * total);
            foreach (var e in entries)
            {
                float w = UnityEngine.Mathf.Max(0, e.Weight);
                if (roll <= w) return e.NextType;
                roll -= w;
            }
            return entries[^1].NextType;
        }

        /// <summary>
        /// 选择某房间类型下的变体索引。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private int PickIndex(RoomType type)
        {
            if (_variantSets != null && _variantSets.TryGetValue(type, out var set))
            {
                var v = set.PickVariant(_rng);
                if (v != null) return v.Index;
            }
            return 0;
        }

        private int PickIndex(RoomType type, int index)
        {
            return index;
        }

        private RoomMeta BuildMeta(RoomType type, int index)
        {
            StepCount++;
            return new RoomMeta
            {
                RoomType = type,
                Index = index,
                BundleName = $"Room_{type}_{index}",
                AvailableExits = Direction.All
            };
        }
    }
}
