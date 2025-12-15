using RogueGame.Map;

/// <summary>
/// GameStateManager 的抽象接口（最小），方便其它系统通过接口与之交互。
/// </summary>
public interface IGameStateManager
{
    void StartRun(RoomMeta meta);
    void EnterRoom(RoomType type, int roomId);
    void ChooseNextRoom();
    void TransitionToNextLayer();
    // 当前层（只读）
    int CurrentLayer { get; }
    /// <summary>
    /// 初始化注入依赖（由组合根 GameManager 调用）。
    /// </summary>
    void Initialize(RogueGame.Map.IReadOnlyRoomRepository roomRepository, RogueGame.Map.IRoomManager roomManager, TransitionController transitionController);
}
