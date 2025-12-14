using UnityEngine;
using System;

namespace CDTU.Utils
{
    /// <summary>
    ///     单例模式基类 - DontDestroyOnLoad 版本
    ///     自动处理多场景环境中的单例创建与销毁
    /// </summary>
    /// <typeparam name="T">继承MonoBehaviour的类型</typeparam>
    public abstract class SingletonDD<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isDestroying = false;

        public static T Instance
        {
            get
            {
                if (_isDestroying)
                {
                    CDTU.Utils.Logger.LogWarning($"[SingletonDD<{typeof(T).Name}>] 单例正在销毁中，无法获取实例");
                    return null;
                }

                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();
                    
                    if (_instance == null && Application.isPlaying)
                    {
                        CDTU.Utils.Logger.Log($"[SingletonDD<{typeof(T).Name}>] 动态创建新实例");
                        var go = new GameObject(typeof(T).Name);
                        _instance = go.AddComponent<T>();
                    }
                }

                return _instance;
            }
            set => _instance = value;
        }

        protected virtual void Awake()
        {
            // 检测多个实例的情况
            T existingInstance = FindFirstObjectByType<T>();
            
            if (existingInstance == null)
            {
                // 这是第一个实例
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                CDTU.Utils.Logger.Log($"[SingletonDD<{typeof(T).Name}>] 创建单例，应用 DontDestroyOnLoad");
            }
            else if (existingInstance != this)
            {
                // 检测到多个实例，销毁新的
                CDTU.Utils.Logger.LogWarning($"[SingletonDD<{typeof(T).Name}>] 检测到多个实例，销毁重复对象：{gameObject.name}");
                Destroy(gameObject);
            }
            else
            {
                // existingInstance == this，说明已经初始化过了
                DontDestroyOnLoad(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (this as T == _instance)
            {
                _isDestroying = true;
                _instance = null;
                CDTU.Utils.Logger.Log($"[SingletonDD<{typeof(T).Name}>] 单例已销毁");
            }
        }
    }
}
