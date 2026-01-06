using System;
using System.Collections.Generic;
using UnityEngine;
using CDTU.Utils;

/// <summary>
/// 特效对象池：按特效预制体创建/管理池，提供 Get/Release/Warmup
/// </summary>
public class VFXPool : SingletonDD<VFXPool>
{
    [Serializable]
    public class VFXConfig
    {
        [Tooltip("特效名称，不填则使用预制体名称")]
        public string vfxName;

        [Tooltip("特效预制体，需包含粒子或其他特效组件")]
        public GameObject prefab;

        [Tooltip("初始预热数量")]
        public int prewarmCount = 2;

        [Tooltip("是否启用重复回收检查")]
        public bool collectionChecks = true;
    }

    private class PoolEntry
    {
        public string name;
        public GameObject prefab;
        public ObjectPool<GameObject> pool;
    }

    [Header("配置")]
    [SerializeField] private int defaultPoolSize = 20;
    [SerializeField] private Transform poolRoot;
    [SerializeField] private List<VFXConfig> presetVfxList = new List<VFXConfig>();

    // 按名称与预制体映射池，便于字符串调用和动态注册
    private readonly Dictionary<string, PoolEntry> _poolsByName = new Dictionary<string, PoolEntry>();
    private readonly Dictionary<GameObject, ObjectPool<GameObject>> _poolsByPrefab = new Dictionary<GameObject, ObjectPool<GameObject>>();
    // 反向映射：实例 -> 对应池，用于 Release 时无需外部记录
    private readonly Dictionary<GameObject, ObjectPool<GameObject>> _instanceToPool = new Dictionary<GameObject, ObjectPool<GameObject>>();

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();

        if (poolRoot == null)
        {
            var root = new GameObject("VFXPool_Root");
            root.transform.SetParent(transform, false);
            poolRoot = root.transform;
        }

        BuildPresetPools();
    }

    protected override void OnSingletonDestroyed()
    {
        ClearAll(true);
        base.OnSingletonDestroyed();
    }

    #region 初始化与注册

    private void BuildPresetPools()
    {
        _poolsByName.Clear();
        _poolsByPrefab.Clear();

        foreach (var config in presetVfxList)
        {
            if (config == null || config.prefab == null) continue;

            var name = string.IsNullOrWhiteSpace(config.vfxName) ? config.prefab.name : config.vfxName;

            if (_poolsByName.ContainsKey(name))
            {
                Debug.LogWarning($"重复的VFX名称: {name}");
                continue;
            }

            var pool = CreatePool(config.prefab, config.prewarmCount, config.collectionChecks);
            var entry = new PoolEntry
            {
                name = name,
                prefab = config.prefab,
                pool = pool
            };

            _poolsByName.Add(name, entry);
            if (!_poolsByPrefab.ContainsKey(config.prefab))
            {
                _poolsByPrefab.Add(config.prefab, pool);
            }
        }
    }

    private ObjectPool<GameObject> CreatePool(GameObject prefab, int size, bool collectionChecks)
    {
        if (prefab == null)
        {
            Debug.LogWarning("尝试为 null 特效创建对象池");
            return null;
        }

        var pool = new ObjectPool<GameObject>(prefab, size > 0 ? size : defaultPoolSize, poolRoot, collectionChecks);
        return pool;
    }

    private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab, string vfxName = null, int? initialSize = null, bool collectionChecks = true)
    {
        if (prefab == null) return null;

        // 按预制体缓存优先
        if (_poolsByPrefab.TryGetValue(prefab, out var pool))
            return pool;

        var name = string.IsNullOrWhiteSpace(vfxName) ? prefab.name : vfxName;

        if (_poolsByName.TryGetValue(name, out var entry))
            return entry.pool;

        pool = CreatePool(prefab, initialSize ?? defaultPoolSize, collectionChecks);
        if (pool != null)
        {
            _poolsByPrefab[prefab] = pool;
            _poolsByName[name] = new PoolEntry { name = name, prefab = prefab, pool = pool };
        }

        return pool;
    }

    #endregion

    #region 对外接口

    /// <summary>
    /// 获取特效实例（按预制体）
    /// </summary>
    public GameObject Get(GameObject prefab, string vfxName = null, Transform parent = null)
    {
        var pool = GetOrCreatePool(prefab, vfxName);
        if (pool == null) return null;

        var instance = pool.Get();
        _instanceToPool[instance] = pool;

        var t = instance.transform;
        t.SetParent(parent != null ? parent : poolRoot, false);

        return instance;
    }

    /// <summary>
    /// 获取特效实例（按名称）
    /// </summary>
    public GameObject Get(string vfxName, Transform parent = null)
    {
        if (string.IsNullOrWhiteSpace(vfxName))
        {
            Debug.LogWarning("VFX 名称为空，无法获取");
            return null;
        }

        if (!_poolsByName.TryGetValue(vfxName, out var entry))
        {
            Debug.LogWarning($"未找到名为 {vfxName} 的 VFX 对象池");
            return null;
        }

        return Get(entry.prefab, vfxName, parent);
    }

    /// <summary>
    /// 回收特效实例
    /// </summary>
    public void Release(GameObject instance)
    {
        if (instance == null) return;

        if (_instanceToPool.TryGetValue(instance, out var pool))
        {
            pool.Release(instance);
            return;
        }

        Debug.LogWarning($"对象 {instance.name} 不属于 VFXPool，直接销毁");
        Destroy(instance);
    }

    /// <summary>
    /// 预热指定特效池
    /// </summary>
    public void Warmup(GameObject prefab, int count, string vfxName = null)
    {
        if (prefab == null) return;

        var pool = GetOrCreatePool(prefab, vfxName, count);
        if (pool == null) return;

        var needMore = Mathf.Max(0, count - pool.CountAll);
        if (needMore > 0)
        {
            pool.Warmup(needMore);
        }
    }

    /// <summary>
    /// 预热指定名称的特效池
    /// </summary>
    public void Warmup(string vfxName, int count)
    {
        if (_poolsByName.TryGetValue(vfxName, out var entry))
        {
            Warmup(entry.prefab, count, vfxName);
        }
    }

    /// <summary>
    /// 预热所有预设特效池
    /// </summary>
    public void WarmupPresets()
    {
        foreach (var config in presetVfxList)
        {
            if (config?.prefab == null) continue;
            var name = string.IsNullOrWhiteSpace(config.vfxName) ? config.prefab.name : config.vfxName;
            Warmup(config.prefab, config.prewarmCount, name);
        }
    }

    /// <summary>
    /// 清理所有池
    /// </summary>
    public void ClearAll(bool destroyActive = false)
    {
        foreach (var kv in _poolsByName)
        {
            kv.Value.pool.Clear(destroyActive);
        }

        _poolsByName.Clear();
        _poolsByPrefab.Clear();
        _instanceToPool.Clear();
    }

    public bool HasPool(string vfxName) => _poolsByName.ContainsKey(vfxName);

    #endregion
}