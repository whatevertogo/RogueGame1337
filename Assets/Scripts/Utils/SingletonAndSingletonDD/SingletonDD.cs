using UnityEngine;

namespace CDTU.Utils
{
    /// <summary>
    /// DontDestroyOnLoad 单例基类（安全版）
    /// 规则：
    /// - Instance 仅返回引用，不调用 Unity API
    /// - 实例只在 Awake 中注册
    /// - 不自动创建、不自动查找
    /// </summary>
    public abstract class SingletonDD<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isQuitting;

        public static T Instance
        {
            get
            {
                if (_instance == null && !_isQuitting)
                {
                    Debug.LogError(
                        $"[SingletonDD<{typeof(T).Name}>] Instance is null. " +
                        $"Ensure the object exists in scene and Awake has been called."
                    );
                }

                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
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

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
