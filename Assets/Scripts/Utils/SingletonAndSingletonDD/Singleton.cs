using UnityEngine;
namespace CDTU.Utils
{
    /// <summary>
    /// 单例基类
    /// 继承自 MonoBehaviour，确保场景中只有一个实例
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _instance;
        private static bool _isQuitting;

        public static T Instance
        {
            get
            {
                if (_isQuitting)
                    return null;

                if (_instance != null)
                    return _instance;

                // 场景中查找
                _instance = FindFirstObjectByType<T>();

                if (_instance != null)
                    return _instance;

                // 不存在则创建
                GameObject go = new GameObject(typeof(T).Name);
                _instance = go.AddComponent<T>();
                _instance.OnSingletonCreated();

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = (T)this;
                OnSingletonAwake();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        /// <summary>
        /// Instance 首次创建时调用（仅一次）
        /// </summary>
        protected virtual void OnSingletonCreated() { }

        /// <summary>
        /// Awake 时调用（包括场景内手动放置）
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        /// <summary>
        /// 销毁阶段（解绑事件）
        /// </summary>
        protected virtual void OnSingletonDestroyed() { }
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }


    }

}