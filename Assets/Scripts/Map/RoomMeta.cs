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

    /// <summary>
    /// 用于运行时的房间实例状态。
    /// 现在包含实例引用与缓存的尺寸信息，便于精确管理生命周期。
    /// </summary>
    public class RoomInstanceState
    {
        public RoomMeta Meta;
        public GameObject Instance;
        public Direction VisitedMask = Direction.None;
        public Vector3 WorldPosition = Vector3.zero;

        /// <summary>
        /// 缓存的房间尺寸（运行时计算后存储）
        /// </summary>
        public Vector2 CachedSize = Vector2.zero;

        public void MarkVisited(Direction dir)
        {
            VisitedMask |= dir;
        }

        public bool IsVisited(Direction dir) => (VisitedMask & dir) != 0;
    }
}