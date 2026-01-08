using System.Threading.Tasks;
using RogueGame.Map;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

// 接口声明异步方法
public interface IRoomLoaderAsync
{
    Task<GameObject> LoadAsync(RoomMeta meta);
}

/// <summary>
/// Addressables 房间加载器
/// 负责通过 Addressables 系统异步加载房间预制体
/// </summary>
public class AddressablesRoomLoader : IRoomLoaderAsync
{
    /// <summary>
    /// 异步加载房间预制体
    /// </summary>
    /// <param name="meta">房间元数据</param>
    /// <returns>加载的预制体，失败返回 null</returns>
    public async Task<GameObject> LoadAsync(RoomMeta meta)
    {
        if (meta == null || string.IsNullOrEmpty(meta.BundleName))
        {
            CDTU.Utils.CDLogger.LogError("[AddressablesRoomLoader] Invalid meta or bundle name");
            return null;
        }

        string address = "Rooms/" + meta.BundleName;
        CDTU.Utils.CDLogger.Log($"[AddressablesRoomLoader] Loading room: {address}");

        AsyncOperationHandle<GameObject> handle = default;

        try
        {
            handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(address);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                CDTU.Utils.CDLogger.Log($"[AddressablesRoomLoader] Successfully loaded: {address}");
                return handle.Result;
            }
            else
            {
                CDTU.Utils.CDLogger.LogError($"[AddressablesRoomLoader] Failed to load: {address}, Status: {handle.Status}");
                UnityEngine.AddressableAssets.Addressables.Release(handle);
                return null;
            }
        }
        catch (System.Exception ex)
        {
            CDTU.Utils.CDLogger.LogError($"[AddressablesRoomLoader] Exception while loading {address}: {ex.Message}");
            UnityEngine.AddressableAssets.Addressables.Release(handle);
            return null;
        }
    }
}
