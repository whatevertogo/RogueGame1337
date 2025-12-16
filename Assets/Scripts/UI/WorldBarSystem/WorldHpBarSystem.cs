using System.Collections.Generic;
using Character.Components;
using CDTU.Utils;
using UnityEngine;
using UnityEngine.Pool;

public class WorldHpBarSystem : MonoBehaviour
{
    public static WorldHpBarSystem Instance { get; private set; }

    [SerializeField] private WorldHpBar barPrefab;
    [SerializeField] private RectTransform barRoot; // should be a UI container under a Canvas
    [SerializeField] private int poolDefaultSize = 0; // 默认不预热，避免启动时大量 Instantiate，可在加载阶段分帧预热

    // use project's generic ObjectPool
    private CDTU.Utils.ObjectPool<WorldHpBar> pool;


    private readonly List<WorldHpBarBinder> binders = new();

    private Camera cachedCamera;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        cachedCamera = Camera.main;
        // initialize pool
        pool = new CDTU.Utils.ObjectPool<WorldHpBar>(barPrefab, poolDefaultSize, barRoot);
    }

    /// <summary>
    /// 分帧预热对象池：在多帧内逐步创建对象以避免卡顿。
    /// total: 目标总数；perFrame: 每帧创建数量。
    /// </summary>
    public void WarmupOverFrames(int total, int perFrame = 2)
    {
        if (total <= 0) return;
        StartCoroutine(WarmupCoroutine(total, Mathf.Max(1, perFrame)));
    }

    private System.Collections.IEnumerator WarmupCoroutine(int total, int perFrame)
    {
        int created = 0;
        while (created < total)
        {
            for (int i = 0; i < perFrame && created < total; i++)
            {
                var obj = pool.Get();
                // 立即回收，确保处于非激活状态
                pool.Release(obj);
                created++;
            }
            yield return null;
        }
    }

    private WorldHpBar RentBar()
    {
        var bar = pool.Get();
        if (bar == null) return null;
        // ensure parent is root
        bar.transform.SetParent(barRoot, false);
        return bar;
    }

    private void ReturnBar(WorldHpBar bar)
    {
        if (bar == null) return;
        pool.Release(bar);
    }

    public WorldHpBarBinder Bind(CharacterStats stats, Transform follow)
    {
        if (stats == null || follow == null) return null;
        var bar = RentBar();
        var binder = new WorldHpBarBinder();
        binder.Bind(stats, follow, bar, barRoot, this);
        binders.Add(binder);
        return binder;
    }

    public void Unbind(WorldHpBarBinder binder)
    {
        if (binder == null) return;
        binder.Unbind();
        binders.Remove(binder);
        if (binder.Bar != null)
        {
            ReturnBar(binder.Bar);
        }
    }

    void LateUpdate()
    {
        if (cachedCamera == null) cachedCamera = Camera.main;
        foreach (var b in binders)
        {
            if (b == null) continue;
            b.LateUpdate(cachedCamera);
        }
    }

    /// <summary>
    /// 获取用于将屏幕点转换为 Canvas 本地点的相机。
    /// 对于 ScreenSpace-Overlay 返回 null；对于 ScreenSpace-Camera/WorldSpace 返回 Canvas.worldCamera（或 cachedCamera 作为回退）。
    /// </summary>
    public Camera GetCanvasCamera()
    {
        if (barRoot == null) return cachedCamera;
        var canvas = barRoot.GetComponentInParent<UnityEngine.Canvas>();
        if (canvas == null) return cachedCamera;
        if (canvas.renderMode == UnityEngine.RenderMode.ScreenSpaceOverlay) return null;
        return canvas.worldCamera != null ? canvas.worldCamera : cachedCamera;
    }
}