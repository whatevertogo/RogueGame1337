using UnityEngine;

namespace CDTU.Utils
{
    /// <summary>
    /// MonoBehaviour 单例基类（安全版）
    /// 规则：
    /// 1. 不在 Instance getter 中调用 Unity API
    /// 2. 实例只在 Awake 中注册
    /// 3. 不负责自动创建 GameObject
    /// 4. 游戏退出时静默返回 null（避免 OnDestroy 中访问报错）
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isQuitting = false;

        public static T Instance
        {
            get
            {
                // 游戏退出时静默返回 null，避免 OnDestroy 中访问报错
                if (_isQuitting) return null;

                if (_instance == null)
                {
                    Debug.LogWarning(
                        $"[Singleton<{typeof(T).Name}>] Instance is null. " +
                        $"Ensure the object exists in scene and Awake has been called."
                    );
                }
                return _instance;
            }
        }

        public static bool HasInstance => _instance != null && !_isQuitting;

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

        /// <summary>
        /// 应用退出时标记，防止 OnDestroy 中访问 Instance 报错
        /// </summary>
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
