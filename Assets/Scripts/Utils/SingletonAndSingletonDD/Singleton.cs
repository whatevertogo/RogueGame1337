using UnityEngine;

namespace CDTU.Utils
{
    /// <summary>
    ///     单例模式基类
    /// </summary>
    /// <typeparam name="T">继承MonoBehaviour的类型</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();
                    if (_instance == null && Application.isPlaying)
                    {
                        var go = new GameObject(typeof(T).Name);
                        _instance = go.AddComponent<T>();
                    }
                }

                return _instance;
            }
            set => _instance = value;
        }

        /// <summary>
        /// 尝试获取已存在的实例，但不会在找不到时自动创建新 GameObject（用于析构/清理阶段避免重新生成实例）
        /// </summary>
        public static T GetExistingInstance()
        {
            if (_instance != null) return _instance;
            return FindFirstObjectByType<T>();
        }

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
    }
}
