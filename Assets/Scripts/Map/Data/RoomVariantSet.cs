using System.Collections.Generic;
using UnityEngine;

namespace RogueGame.Map.Data
{
    /// <summary>
    /// 某房间类型下的 prefab/变体列表（用于 Room_<Type>_<Index> 选择）。
    /// </summary>
    [CreateAssetMenu(fileName = "RoomVariantSet", menuName = "RogueGame/Map/Room Variant Set")]
    public class RoomVariantSet : ScriptableObject
    {
        [System.Serializable]
        public class Variant
        {
            public int Index;
            public float Weight = 1f;
        }

        public RogueGame.Map.RoomType RoomType;
        public List<Variant> Variants = new();

        public Variant PickVariant(System.Random rng)
        {
            if (Variants == null || Variants.Count == 0) return null;
            float total = 0f;
            foreach (var v in Variants) total += Mathf.Max(0, v.Weight);
            if (total <= 0f) return Variants[0];
            float roll = (float)(rng.NextDouble() * total);
            foreach (var v in Variants)
            {
                float w = Mathf.Max(0, v.Weight);
                if (roll <= w) return v;
                roll -= w;
            }
            return Variants[^1];
        }
    }
}
