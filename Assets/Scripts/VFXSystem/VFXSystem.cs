using UnityEngine;
using CDTU.Utils;

/// <summary>
/// 特效系统：封装播放/预热逻辑，对外屏蔽对象池细节
/// </summary>
public class VFXSystem : SingletonDD<VFXSystem>
{
    [Header("依赖")]
    [SerializeField] private VFXPool vfxPool;

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();
        EnsurePool();
    }

    private VFXPool EnsurePool()
    {
        if (vfxPool == null)
            vfxPool = VFXPool.Instance;
        return vfxPool;
    }

    /// <summary>
    /// 播放特效（自动回收）
    /// </summary>
    /// <param name="prefab">特效预制体</param>
    /// <param name="position">播放位置</param>
    /// <param name="rotation">播放朝向</param>
    /// <param name="parent">可选父节点</param>
    /// <param name="durationOverride">自定义存活时间（秒），<=0 自动计算</param>
    /// <param name="resetScale">是否将缩放重置为 Vector3.one</param>
    public GameObject Play(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, float durationOverride = -1f, bool resetScale = true)
    {
        if (prefab == null)
        {
            Debug.LogWarning("VFXSystem.Play 收到空的特效预制体");
            return null;
        }

        var pool = EnsurePool();
        if (pool == null)
        {
            Debug.LogError("VFXPool 未初始化，无法播放特效");
            return null;
        }

        var instance = pool.Get(prefab, null, parent);
        if (instance == null) return null;

        var t = instance.transform;
        t.SetPositionAndRotation(position, rotation);
        if (resetScale) t.localScale = Vector3.one;

        // 重新播放粒子与动画
        var particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            ps.Clear(true);
            ps.Simulate(0f, true, true);
            ps.Play(true);
        }

        var autoRelease = instance.GetComponent<VFXAutoRelease>();
        if (autoRelease == null)
            autoRelease = instance.AddComponent<VFXAutoRelease>();

        autoRelease.Init(pool, prefab, durationOverride);

        return instance;
    }

    /// <summary>
    /// 便捷：仅传位置
    /// </summary>
    public GameObject PlayAt(GameObject prefab, Vector3 position, Transform parent = null, float durationOverride = -1f)
    {
        return Play(prefab, position, Quaternion.identity, parent, durationOverride);
    }

    /// <summary>
    /// 按名称播放（需在 VFXPool 里注册或预设）
    /// </summary>
    public GameObject Play(string vfxName, Vector3 position, Quaternion rotation, Transform parent = null, float durationOverride = -1f, bool resetScale = true)
    {
        var pool = EnsurePool();
        if (pool == null)
        {
            Debug.LogError("VFXPool 未初始化，无法播放特效");
            return null;
        }

        var instance = pool.Get(vfxName, parent);
        if (instance == null) return null;

        var t = instance.transform;
        t.SetPositionAndRotation(position, rotation);
        if (resetScale) t.localScale = Vector3.one;

        var particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            ps.Clear(true);
            ps.Simulate(0f, true, true);
            ps.Play(true);
        }

        var autoRelease = instance.GetComponent<VFXAutoRelease>();
        if (autoRelease == null)
            autoRelease = instance.AddComponent<VFXAutoRelease>();

        autoRelease.Init(pool, instance, durationOverride);

        return instance;
    }

    public GameObject PlayAt(string vfxName, Vector3 position, Transform parent = null, float durationOverride = -1f)
    {
        return Play(vfxName, position, Quaternion.identity, parent, durationOverride);
    }

    /// <summary>
    /// 预热指定特效
    /// </summary>
    public void Warmup(GameObject prefab, int count)
    {
        EnsurePool()?.Warmup(prefab, count);
    }

    /// <summary>
    /// 预热指定名称特效
    /// </summary>
    public void Warmup(string vfxName, int count)
    {
        EnsurePool()?.Warmup(vfxName, count);
    }

    /// <summary>
    /// 手动回收实例
    /// </summary>
    public void Release(GameObject instance)
    {
        EnsurePool()?.Release(instance);
    }
}