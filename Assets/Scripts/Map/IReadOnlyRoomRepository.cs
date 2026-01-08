using System.Collections.Generic;

namespace RogueGame.Map
{
    /// <summary>
    /// 只读的房间仓库接口，提供查询房间元数据与实例状态的方法。
    /// 由 RoomManager 实现并对外暴露只读视图。
    /// </summary>
    public interface IReadOnlyRoomRepository
    {
        RoomInstanceState GetInstance(int instanceId);
        bool TryGetInstance(int instanceId, out RoomInstanceState instance);
        IEnumerable<RoomInstanceState> GetAllInstances();
        IEnumerable<RoomInstanceState> GetInstancesOnFloor(int floor);
        IEnumerable<RoomInstanceState> GetUnvisitedOnFloor(int floor);
    }
}
