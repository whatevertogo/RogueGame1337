using UnityEngine;

namespace RogueGame.Map.Loading
{
    /// <summary>
    /// 开发阶段默认使用 Resources 加载房间 prefab。
    /// 期待路径：Resources/Rooms/Room_<Type>_<Index>
    /// </summary>
    public class ResourcesRoomLoader : IRoomLoader
    {
        private const string DefaultPathPrefix = "Rooms";

        public GameObject Load(RoomMeta meta)
        {
            if (meta == null || string.IsNullOrEmpty(meta.BundleName)) return null;
            var path = DefaultPathPrefix + "/" + meta.BundleName;
            return Resources.Load<GameObject>(path);
        }
    }
}
