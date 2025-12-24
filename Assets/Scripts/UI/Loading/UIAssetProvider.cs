using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UI.Loading
{
    public class UIAssetProvider
    {
        public async Task<GameObject> LoadAsync<T>() where T : UIViewBase
        {
            string address = "UI/" + typeof(T).Name;
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);

            try
            {
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    // 成功返回 Prefab，不释放 handle，让调用方管理或缓存
                    return handle.Result;
                }
                else
                {
                    CDTU.Utils.CDLogger.LogError($"UIAssetProvider: Failed to load {typeof(T).Name}, Status: {handle.Status}");
                    // 加载失败时释放
                    if (handle.IsValid()) Addressables.Release(handle);
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                CDTU.Utils.CDLogger.LogError($"UIAssetProvider: 加载 {typeof(T).Name} 时发生异常: {ex.Message}");
                if (handle.IsValid()) Addressables.Release(handle);
                return null;
            }
        }
    }
}
