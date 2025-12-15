using RogueGame.Map;

namespace RogueGame.Map
{
    /// <summary>
    /// RoomManager 的抽象接口，便于上层通过接口依赖而非具体类型耦合实现。
    /// 只暴露 GameStateManager 当前需要的最小成员。
    /// </summary>
    public interface IRoomManager : IReadOnlyRoomRepository
    {
        RoomInstanceState CurrentRoom { get; }
        void StartRun(RoomMeta startMeta);
        void SwitchToNextRoom(Direction exitDir);
        bool TryEnterDoor(Direction dir);
        // 启动指定层的运行（由 GameStateManager 调用）
        void StartFloor(int floor, RoomMeta startMeta);
        // 返回解锁 Boss 所需清理战斗房间阈值
        int GetBossUnlockThreshold();
    }
}
