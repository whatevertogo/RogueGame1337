using UnityEngine;

namespace RogueGame.Map.Loading
{
    /// <summary>
    /// AssetBundle 加载占位实现：留接口，未接入时返回 null。
    /// </summary>
    public sealed class AssetBundleRoomLoader : IRoomLoader
    {
        public GameObject Load(RogueGame.Map.RoomMeta meta)
        {
            // TODO: 接入 AssetBundle 加载逻辑。当前占位返回 null。
            return null;
        }
    }
}
