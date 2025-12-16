using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CardSystem
{
    /// <summary>
    /// 全局卡牌定义注册表：通过 cardId 查找 CardData(SO)
    /// 运行时自动初始化；编辑器下会扫描工程中的所有 CardData 并做唯一性检查。
    /// </summary>
    public static class CardRegistry
    {
        private static readonly Dictionary<string, CardData> _map = new Dictionary<string, CardData>();
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;
            _map.Clear();

#if UNITY_EDITOR
            // 编辑器：使用 AssetDatabase 扫描所有 CardData 资源
            var guids = AssetDatabase.FindAssets("t:CardData");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var data = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (data == null) continue;
                if (string.IsNullOrEmpty(data.cardId))
                {
                    Debug.LogWarning($"[CardRegistry] CardData at '{path}' has empty cardId.");
                    continue;
                }

                if (_map.ContainsKey(data.cardId))
                {
                    Debug.LogError($"[CardRegistry] Duplicate cardId '{data.cardId}' found in '{path}' and another asset");
                }
                else
                {
                    _map[data.cardId] = data;
                }
            }
#else
            // 运行时：尝试从 Resources 加载（要求把 CardData 放到 Resources 下），否则日志提示
            var all = Resources.LoadAll<CardData>("");
            if (all == null || all.Length == 0)
            {
                Debug.LogWarning("[CardRegistry] No CardData found in Resources. If you don't use Resources, consider calling CardRegistry.Register manually or run editor scan.");
            }
            else
            {
                foreach (var data in all)
                {
                    if (data == null || string.IsNullOrEmpty(data.cardId)) continue;
                    if (!_map.ContainsKey(data.cardId))
                        _map[data.cardId] = data;
                }
            }
#endif

            _initialized = true;
        }

        public static CardData Resolve(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;
            Initialize();
            _map.TryGetValue(cardId, out var data);
            return data;
        }

        public static bool TryResolve(string cardId, out CardData data)
        {
            data = Resolve(cardId);
            return data != null;
        }

        public static IReadOnlyCollection<CardData> GetAllDefinitions()
        {
            Initialize();
            return _map.Values;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EditorInit()
        {
            Initialize();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInit()
        {
            Initialize();
        }
    }
}
