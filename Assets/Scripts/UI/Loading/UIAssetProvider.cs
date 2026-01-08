using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UI.Loading
{
    /// <summary>
    /// UI 资源加载器（优化版）
    /// - Prefab 缓存：避免重复加载
    /// - Handle 管理：防止内存泄漏
    /// - 引用计数：安全释放资源
    /// </summary>
    public class UIAssetProvider
    {
        /// <summary>
        /// 缓存条目：包含 Prefab、Handle 和引用计数
        /// </summary>
        private class CacheEntry
        {
            public GameObject Prefab;
            public AsyncOperationHandle<GameObject> Handle;
            public int RefCount;
        }

        /// <summary>
        /// Prefab 缓存（Type -> CacheEntry）
        /// </summary>
        private readonly Dictionary<Type, CacheEntry> _cache = new();

        /// <summary>
        /// 正在加载中的任务（防止重复加载）
        /// </summary>
        private readonly Dictionary<Type, UniTask<GameObject>> _loadingTasks = new();

        /// <summary>
        /// 异步加载 UI Prefab（带缓存）
        /// </summary>
        public async UniTask<GameObject> LoadAsync<T>()
            where T : UIViewBase
        {
            Type type = typeof(T);

            // 1. 检查缓存
            if (_cache.TryGetValue(type, out var entry))
            {
                entry.RefCount++;
                return entry.Prefab;
            }

            // 2. 检查是否正在加载（避免重复加载）
            if (_loadingTasks.TryGetValue(type, out var existingTask))
            {
                return await existingTask;
            }

            // 3. 开始新加载
            var loadTask = LoadInternalAsync<T>();
            _loadingTasks[type] = loadTask;

            try
            {
                var prefab = await loadTask;
                return prefab;
            }
            finally
            {
                _loadingTasks.Remove(type);
            }
        }

        /// <summary>
        /// 内部加载实现
        /// </summary>
        private async UniTask<GameObject> LoadInternalAsync<T>()
            where T : UIViewBase
        {
            Type type = typeof(T);
            string address = "UI/" + type.Name;
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(
                address
            );

            try
            {
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    // 缓存成功加载的 Prefab
                    _cache[type] = new CacheEntry
                    {
                        Prefab = handle.Result,
                        Handle = handle,
                        RefCount = 1,
                    };
                    return handle.Result;
                }
                else
                {
                    CDTU.Utils.CDLogger.LogError(
                        $"[UIAssetProvider] 加载失败: {type.Name}, Status: {handle.Status}"
                    );
                    if (handle.IsValid())
                        Addressables.Release(handle);
                    return null;
                }
            }
            catch (Exception ex)
            {
                CDTU.Utils.CDLogger.LogError(
                    $"[UIAssetProvider] 加载 {type.Name} 时发生异常: {ex.Message}"
                );
                if (handle.IsValid())
                    Addressables.Release(handle);
                return null;
            }
        }

        /// <summary>
        /// 释放 UI 资源（引用计数 -1，归零时释放）
        /// </summary>
        public void Release<T>()
            where T : UIViewBase
        {
            Release(typeof(T));
        }

        /// <summary>
        /// 释放 UI 资源（按类型）
        /// </summary>
        public void Release(Type type)
        {
            if (!_cache.TryGetValue(type, out var entry))
                return;

            entry.RefCount--;

            // 引用计数归零时释放资源
            if (entry.RefCount <= 0)
            {
                if (entry.Handle.IsValid())
                {
                    Addressables.Release(entry.Handle);
                }
                _cache.Remove(type);
                CDTU.Utils.CDLogger.Log($"[UIAssetProvider] 已释放: {type.Name}");
            }
        }

        /// <summary>
        /// 强制释放所有缓存（场景切换时调用）
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var kvp in _cache)
            {
                if (kvp.Value.Handle.IsValid())
                {
                    Addressables.Release(kvp.Value.Handle);
                }
            }
            _cache.Clear();
            _loadingTasks.Clear();
            CDTU.Utils.CDLogger.Log("[UIAssetProvider] 已释放所有缓存");
        }

        /// <summary>
        /// 预加载 UI（用于提前加载常用 UI）
        /// </summary>
        public async UniTask PreloadAsync<T>()
            where T : UIViewBase
        {
            await LoadAsync<T>();
            CDTU.Utils.CDLogger.Log($"[UIAssetProvider] 预加载完成: {typeof(T).Name}");
        }

        /// <summary>
        /// 获取缓存状态（调试用）
        /// </summary>
        public string GetDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== UIAssetProvider 缓存状态 ({_cache.Count} 项) ===");
            foreach (var kvp in _cache)
            {
                sb.AppendLine($"  {kvp.Key.Name}: RefCount={kvp.Value.RefCount}");
            }
            return sb.ToString();
        }
    }
}
