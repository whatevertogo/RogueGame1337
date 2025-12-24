using UnityEngine;

namespace CDTU.Utils
{
    /// <summary>
    /// Logger：在 Editor 总是输出，打包后根据 Build 类型输出。
    /// - Editor: 所有日志输出
    /// - Development Build: 所有日志输出
    /// - Release Build: 仅 Warning 和 Error 输出
    /// </summary>
    public static class CDLogger
    {
        public static void Log(object message, Object context = null)
        {
#if UNITY_EDITOR
            // Editor 中总是输出 Log
            if (context != null) Debug.Log($"{message}", context);
            else Debug.Log($"{message}");
#elif DEVELOPMENT_BUILD
            // Development Build 中输出 Log
            if (context != null) Debug.Log($"[Dev] {message}", context);
            else Debug.Log($"[Dev] {message}");
#endif
            // Release Build 中不输出 Log
        }

        public static void LogWarning(object message, Object context = null)
        {
            // Warning 在所有情况下都输出
            if (context != null) Debug.LogWarning($"{message}", context);
            else Debug.LogWarning($"{message}");
        }

        public static void LogError(object message, Object context = null)
        {
            if (context != null) Debug.LogError($"{message}", context);
            else Debug.LogError($"{message}");
        }
    }
}
