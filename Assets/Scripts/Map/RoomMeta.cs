using UnityEngine;

namespace RogueGame.Map
{
    /// <summary>
    /// 房间元数据：描述类型、资源名、索引、标记与尺寸。
    /// </summary>
    [System.Serializable]
    public class RoomMeta
    {
        public RoomType RoomType;
        public int Index;
        public string BundleName; // Room_<Type>_<Index>
        public Direction AvailableExits = Direction.All;

        [Header("房间尺寸（0 表示使用 RoomPrefab 自动计算或默认值）")]
        public float Width = 0f;
        public float Height = 0f;

        /// <summary>
        /// 是否有手动配置的尺寸
        /// </summary>
        public bool HasCustomSize => Width > 0 && Height > 0;

        public RoomMeta Clone()
        {
            return (RoomMeta)MemberwiseClone();
        }
    }

    
}