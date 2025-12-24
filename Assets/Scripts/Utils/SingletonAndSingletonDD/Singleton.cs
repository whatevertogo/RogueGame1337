using UnityEngine;

namespace CDTU.Utils
{
    /// <summary>
    /// MonoBehaviour 单例基类（安全版）
    /// 规则：
    /// 1. 不在 Instance getter 中调用 Unity API
    /// 2. 实例只在 Awake 中注册
    /// 3. 不负责自动创建 GameObject
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError(
                        $"[Singleton<{typeof(T).Name}>] Instance is null. " +
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
        }
    }
}
