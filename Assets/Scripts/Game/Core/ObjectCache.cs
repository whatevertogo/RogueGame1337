using System.Collections.Generic;
using UnityEngine;
using CDTU.Utils;
using Character;

namespace Core
{
    /// <summary>
    /// 对象缓存系统 - 避免频繁的FindObjectsOfType调用
    /// </summary>
    public class ObjectCache : Singleton<ObjectCache>
    {
        private Dictionary<System.Type, UnityEngine.Object[]> _cachedObjects = new Dictionary<System.Type, UnityEngine.Object[]>();
        private Dictionary<string, UnityEngine.Object[]> _cachedTagObjects = new Dictionary<string, UnityEngine.Object[]>();
        private bool _cacheDirty = true;
        
        /// <summary>
        /// 标记缓存为脏数据，下次访问时重新构建
        /// </summary>
        public void MarkDirty()
        {
            _cacheDirty = true;
        }
        
        /// <summary>
        /// 获取指定类型的所有对象（使用缓存）
        /// 注意：方法名与 UnityEngine.Object.FindObjectsOfType 相同，使用 new 显式隐藏基类成员以避免编译器警告。
        /// </summary>
        public new T[] FindObjectsOfType<T>() where T : UnityEngine.Object
        {
            var type = typeof(T);
            
            if (_cacheDirty || !_cachedObjects.ContainsKey(type))
            {
                RebuildCache();
            }
            
            if (_cachedObjects.TryGetValue(type, out var objects))
            {
                var result = new T[objects.Length];
                for (int i = 0; i < objects.Length; i++)
                {
                    result[i] = objects[i] as T;
                }
                return result;
            }
            
            return new T[0];
        }
        
        /// <summary>
        /// 获取指定标签的所有对象（使用缓存）
        /// </summary>
        public GameObject[] FindGameObjectsWithTag(string tag)
        {
            if (_cacheDirty || !_cachedTagObjects.ContainsKey(tag))
            {
                RebuildCache();
            }
            
            if (_cachedTagObjects.TryGetValue(tag, out var objects))
            {
                var result = new GameObject[objects.Length];
                for (int i = 0; i < objects.Length; i++)
                {
                    result[i] = objects[i] as GameObject;
                }
                return result;
            }
            
            return new GameObject[0];
        }
        
        /// <summary>
        /// 重建整个缓存
        /// </summary>
        private void RebuildCache()
        {
            _cachedObjects.Clear();
            _cachedTagObjects.Clear();
            
            // 缓存常用类型
            CacheObjects<CharacterBase>();
            CacheObjects<CardSlot>();
            
            _cacheDirty = false;
            CDTU.Utils.CDLogger.Log("[ObjectCache] 缓存重建完成");
        }
        
        /// <summary>
        /// 缓存指定类型的对象
        /// </summary>
        private void CacheObjects<T>() where T : UnityEngine.Object
        {
            var type = typeof(T);
            var objects = Resources.FindObjectsOfTypeAll<T>();
            
            if (objects != null && objects.Length > 0)
            {
                var objectArray = new UnityEngine.Object[objects.Length];
                for (int i = 0; i < objects.Length; i++)
                {
                    objectArray[i] = objects[i];
                }
                _cachedObjects[type] = objectArray;
            }
        }
        
        /// <summary>
        /// 强制刷新特定类型的缓存
        /// </summary>
        public void RefreshType<T>() where T : UnityEngine.Object
        {
            var type = typeof(T);
            if (_cachedObjects.ContainsKey(type))
            {
                CacheObjects<T>();
                CDTU.Utils.CDLogger.Log($"[ObjectCache] 刷新类型缓存: {type.Name}");
            }
        }
        
        /// <summary>
        /// 清理缓存
        /// </summary>
        public void ClearCache()
        {
            _cachedObjects.Clear();
            _cachedTagObjects.Clear();
            _cacheDirty = true;
            CDTU.Utils.CDLogger.Log("[ObjectCache] 缓存已清理");
        }
        
        private void OnEnable()
        {
            // 订阅场景加载事件来标记缓存为脏数据
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            MarkDirty();
        }
    }
}