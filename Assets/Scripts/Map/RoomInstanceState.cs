using RogueGame.Map;
using UnityEngine;

/// <summary>
/// 用于运行时的房间实例状态。
/// 现在包含实例引用与缓存的尺寸信息，便于精确管理生命周期。
/// </summary>
public class RoomInstanceState
    {
        public int InstanceId;
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