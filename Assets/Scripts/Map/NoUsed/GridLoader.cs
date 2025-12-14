// using System.Collections.Generic;
// using UnityEngine;

// namespace RogueGame.Map
// {
//     /// <summary>
//     /// 可选的装饰层 Tilemap 加载器。
//     /// 
//     /// 如果你需要在房间之上叠加额外的装饰层（例如：环境特效、动态障碍物层），
//     /// 可以使用此 GridLoader 按 RoomMeta.BundleName 加载对应的额外 tilemap。
//     /// 
//     /// 约定：Resources 路径前缀（默认 "Tilemaps/"） + RoomMeta.BundleName
//     /// 例如 Resources/Tilemaps/Room_Normal_0
//     ///
//     /// 功能：
//     /// - 按 RoomMeta 加载对应的 Tilemap prefab（同步 Resources.Load）
//     /// - 支持简单缓存复用（避免重复实例化）
//     /// - 提供 LoadForRoom/UnloadCurrent/ReleaseAll 接口供外部（如 RoomManager）调用
//     ///
//     /// 注意：RoomManager 已集成可选调用，在 Inspector 中指定 GridLoader 字段即可启用。
//     /// </summary>
//     public class GridLoader : MonoBehaviour
//     {
//         [Header("Config")]
//         [Tooltip("Resources 中的路径前缀，例如: \"Tilemaps/\"")]
//         public string PathPrefix = "Tilemaps/";

//         [Header("Roots")]
//         [Tooltip("父级，用于放置当前加载的 tilemap 实例")]
//         public Transform GridRoot;

//         [Header("Pooling")]
//         [Tooltip("是否启用简单缓存复用（缓存已实例化的 prefab 实例）")]
//         public bool UseCache = true;

//         private readonly Dictionary<string, GameObject> _cache = new();
//         private GameObject _currentInstance;
//         private string _currentKey;

//         /// <summary>
//         /// 根据 RoomMeta 加载对应的 Tilemap 预制体并返回实例（若已缓存则复用）。
//         /// 返回值：实例 GameObject，失败返回 null。
//         /// </summary>
//         public GameObject LoadForRoom(RoomMeta meta)
//         {
//             if (meta == null || string.IsNullOrEmpty(meta.BundleName)) return null;
//             var key = meta.BundleName;

//             // 若和当前相同，直接返回
//             if (_currentKey == key && _currentInstance != null) return _currentInstance;

//             // 隐藏当前实例
//             if (_currentInstance != null) _currentInstance.SetActive(false);

//             // 尝试从缓存取
//             if (UseCache && _cache.TryGetValue(key, out var cached) && cached != null)
//             {
//                 _currentInstance = cached;
//                 _currentKey = key;
//                 _currentInstance.SetActive(true);
//                 return _currentInstance;
//             }

//             // 同步加载 prefab
//             var path = PathPrefix + key; // e.g. "Tilemaps/Room_Normal_0"
//             var prefab = Resources.Load<GameObject>(path);
//             if (prefab == null)
//             {
//                 Debug.LogWarning($"GridLoader: cannot find tilemap prefab at Resources/{path}");
//                 _currentInstance = null;
//                 _currentKey = null;
//                 return null;
//             }

//             var go = Instantiate(prefab);
//             go.name = key + "_Tilemap"; // 方便层级查找
//             if (GridRoot != null) go.transform.SetParent(GridRoot, false);

//             _currentInstance = go;
//             _currentKey = key;

//             if (UseCache)
//             {
//                 // 缓存实例以复用
//                 _cache[key] = go;
//             }

//             return _currentInstance;
//         }

//         /// <summary>
//         /// 隐藏/卸载当前实例（若启用缓存则仅隐藏）。
//         /// </summary>
//         public void UnloadCurrent()
//         {
//             if (_currentInstance == null) return;
//             if (UseCache)
//             {
//                 _currentInstance.SetActive(false);
//             }
//             else
//             {
//                 Destroy(_currentInstance);
//                 // 从缓存中剔除（防御）
//                 if (!string.IsNullOrEmpty(_currentKey)) _cache.Remove(_currentKey);
//             }
//             _currentInstance = null;
//             _currentKey = null;
//         }

//         /// <summary>
//         /// 释放所有缓存（会 Destroy 实例），并清空当前引用。
//         /// </summary>
//         public void ReleaseAll()
//         {
//             foreach (var kv in _cache)
//             {
//                 if (kv.Value != null) Destroy(kv.Value);
//             }
//             _cache.Clear();
//             _currentInstance = null;
//             _currentKey = null;
//         }

//         private void OnDestroy()
//         {
//             // 清理以避免内存泄漏
//             ReleaseAll();
//         }
//     }
// }
