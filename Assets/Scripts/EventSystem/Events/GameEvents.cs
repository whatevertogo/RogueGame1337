using RogueGame.Map;

namespace RogueGame.Events
{
    /// <summary>
    /// 简单的游戏事件数据契约
    /// </summary>
    public class RoomEnteredEvent
    {
        public int RoomId;
        public int InstanceId;
        public RoomType RoomType;
    }

    public class CombatStartedEvent
    {
        public int RoomId;
        public int InstanceId;
        public RoomType RoomType;
    }

    public class RoomClearedEvent
    {
        public int RoomId;
        public int InstanceId;
        public RoomType RoomType;
        public int ClearedEnemyCount;
    }

    public class ChooseNextRoomEvent
    {
        public int FromRoomId;
        public int FromRoomInstanceId;
    }

    public class LayerTransitionEvent
    {
        public int FromLayer;
        public int ToLayer;
    }

    public class DoorEnterRequestedEvent
    {
        public Direction Direction;
        public int RoomId;
        public int InstanceId;
    }

    public class StartRunRequestedEvent
    {
        public RogueGame.Map.RoomMeta StartMeta;
        public int InitialRoomId;
    }

    // 由 GameStateManager 处理奖励选择逻辑（从事实事件触发）
    public class RewardSelectionRequestedEvent
    {
        public int RoomId;
        public int InstanceId;
        public RoomType RoomType;
    }

    public class RewardGrantedEvent
    {
        public int RoomId;
        public int InstanceId;
        public string RewardId; // 简化的奖励标识
    }
}