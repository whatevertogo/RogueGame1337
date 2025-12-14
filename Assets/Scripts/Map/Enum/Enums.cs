using System;

namespace RogueGame.Map
{
    /// <summary>
    /// 四方向位掩码
    /// </summary>
    [Flags]
    public enum Direction
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3,
        All = North | East | South | West
    }

    public static class DirectionUtils
    {
        public static Direction Opposite(Direction dir)
        {
            return dir switch
            {
                Direction.North => Direction.South,
                Direction.South => Direction.North,
                Direction.East => Direction.West,
                Direction.West => Direction.East,
                _ => Direction. None
            };
        }

        public static UnityEngine.Vector3 ToVector3(Direction dir)
        {
            return dir switch
            {
                Direction.North => UnityEngine.Vector3.up,
                Direction.South => UnityEngine.Vector3.down,
                Direction.East => UnityEngine.Vector3.right,
                Direction.West => UnityEngine.Vector3.left,
                _ => UnityEngine.Vector3.zero
            };
        }
    }

    /// <summary>
    /// 门状态
    /// </summary>
    public enum DoorState
    {
        Closed,     // 关闭（战斗中）
        Open,       // 开启（可通过）
        Locked,     // 锁定（需要钥匙）
        Hidden      // 隐藏（无门）
    }

    /// <summary>
    /// 房间类型
    /// </summary>
    public enum RoomType
    {
        Start,      // 起始房
        Normal,     // 普通战斗
        Elite,      // 精英
        Shop,       // 商店
        Treasure,   // 宝藏
        Boss,       // Boss
        Safe        // 安全房
    }

    /// <summary>
    /// 房间状态
    /// </summary>
    public enum RoomState
    {
        Inactive,   // 未激活（玩家未进入）
        Idle,       // 空闲（非战斗或已清理）
        Combat,     // 战斗中
        Cleared,    // 已清理
        Completed   // 完成（已领取奖励）
    }
}