using System.Collections.Generic;
using UnityEngine;
using CDTU.Utils;

namespace Character.Projectiles
{
    /// <summary>
    /// 投射物对象池
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool Instance { get; private set; }

        [Header("配置")]
        [SerializeField] private int defaultPoolSize = 20;
        [SerializeField] private bool showDebugInfo = false;

        // 按预制体分类的池
        private readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();

        // 实例到预制体的映射（用于归还时查找）
        private readonly Dictionary<int, GameObject> _instanceToPrefab = new();

        // 统计
        private int _totalGet = 0;
        private int _totalReturn = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            ClearAll();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 预热
        /// </summary>
        public void Warmup(GameObject prefab, int count)
        {
            if (prefab == null) return;

            var pool = GetOrCreatePool(prefab);
            int needMore = count - pool.CountAll;

            if (needMore > 0)
            {
                pool.Warmup(needMore);
            }

            if (showDebugInfo)
            {
                Debug.Log($"[ProjectilePool] 预热 {prefab.name}:  {pool.CountAll} 个");
            }
        }

        /// <summary>
        /// 获取投射物
        /// </summary>
        public ProjectileBase Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            var pool = GetOrCreatePool(prefab);
            var go = pool.Get();

            if (go == null) return null;

            // 设置位置和父级
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.SetParent(transform);

            // 记录实例到预制体的映射
            int instanceId = go.GetInstanceID();
            _instanceToPrefab[instanceId] = prefab;

            _totalGet++;

            return go.GetComponent<ProjectileBase>();
        }

        /// <summary>
        /// 归还投射物
        /// </summary>
        public void Return(ProjectileBase projectile)
        {
            if (projectile == null) return;

            var go = projectile.gameObject;
            int instanceId = go.GetInstanceID();

            // 清除拖尾
            var trail = go.GetComponentInChildren<TrailRenderer>();
            trail?.Clear();

            // 查找对应的池
            if (_instanceToPrefab.TryGetValue(instanceId, out var prefab))
            {
                if (_pools.TryGetValue(prefab, out var pool))
                {
                    go.transform.SetParent(transform);
                    pool.Release(go);
                    _totalReturn++;
                    return;
                }
            }

            // 找不到池，销毁
            if (showDebugInfo)
            {
                Debug.LogWarning($"[ProjectilePool] 找不到 {go.name} 的对象池");
            }
            Destroy(go);
        }

        /// <summary>
        /// 清空所有池
        /// </summary>
        public void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear(destroyActive: true);
            }
            _pools.Clear();
            _instanceToPrefab.Clear();
            _totalGet = 0;
            _totalReturn = 0;
        }

        private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new ObjectPool<GameObject>(
                    prefab: prefab,
                    defaultSize: defaultPoolSize,
                    parent: transform,
                    collectionChecks: true
                );
                _pools[prefab] = pool;

                if (showDebugInfo)
                {
                    Debug.Log($"[ProjectilePool] 创建池:  {prefab.name}");
                }
            }
            return pool;
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(10, 10, 200, 120));
            GUILayout.BeginVertical("box");

            GUILayout.Label("=== 投射物池 ===");
            GUILayout.Label($"获取: {_totalGet} | 归还: {_totalReturn}");

            foreach (var kvp in _pools)
            {
                var p = kvp.Value;
                GUILayout.Label($"  {kvp.Key.name}: {p.CountActive}/{p.CountAll}");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif
    }
}