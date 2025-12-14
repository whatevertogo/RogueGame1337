using RogueGame.Map;

namespace RogueGame.Events
{
    /// <summary>
    /// 简单的游戏事件数据契约
    /// </summary>
    public class RoomEnteredEvent
    {
        public int RoomId;
        public RoomType RoomType;
    }

    public class CombatStartedEvent
    {
        public int RoomId;
        public RoomType RoomType;
    }

    public class RoomClearedEvent
    {
        public int RoomId;
        public RoomType RoomType;
        public int ClearedEnemyCount;
    }

    public class ChooseNextRoomEvent
    {
        public int FromRoomId;
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
    }

    public class StartRunRequestedEvent
    {
        public RogueGame.Map.RoomMeta StartMeta;
        public int InitialRoomId;
    }
}