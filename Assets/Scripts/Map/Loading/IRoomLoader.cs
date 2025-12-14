using UnityEngine;
using RogueGame.Map;

namespace RogueGame.Map.Loading
{
    public interface IRoomLoader
    {
        GameObject Load(RoomMeta meta);
    }
}
