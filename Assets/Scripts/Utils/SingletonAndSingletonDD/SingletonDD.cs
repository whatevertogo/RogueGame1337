using UnityEngine;

namespace CDTU.Utils
{
    /// <summary>
    /// DontDestroyOnLoad 懒加载单例基类（工程安全版）
    /// 特性：
    /// - 第一次访问 Instance 时创建
    /// - 优先复用场景中已存在实例
    /// - 自动 DontDestroyOnLoad
    /// - 防止退出阶段误创建
    /// </summary>
    public abstract class SingletonDD<T> : MonoBehaviour
        where T : SingletonDD<T>
    {
        protected static T _instance;
        private static bool _isQuitting;

        public static T Instance
        {
            get
            {
                if (_isQuitting)
                    return null;

                if (_instance != null)
                    return _instance;

                // 1️⃣ 尝试从场景中查找
                _instance = FindFirstObjectByType<T>();

                if (_instance != null)
                {
                    DontDestroyOnLoad(_instance.gameObject);
                    return _instance;
                }

                // 2️⃣ 创建新对象
                GameObject go = new GameObject(typeof(T).Name);
                _instance = go.AddComponent<T>();
                DontDestroyOnLoad(go);

                _instance.OnSingletonCreated();
                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = (T)this;
                DontDestroyOnLoad(gameObject);
                OnSingletonAwake();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            OnSingletonDestroyed();
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        /// <summary>
        /// 懒加载创建时调用（仅一次）
        /// </summary>
        protected virtual void OnSingletonCreated() { }

        /// <summary>
        /// Awake 阶段调用（包括场景预放置）
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        /// <summary>
        /// 销毁阶段（解绑事件）
        /// </summary>
        protected virtual void OnSingletonDestroyed() { }
    }
}
