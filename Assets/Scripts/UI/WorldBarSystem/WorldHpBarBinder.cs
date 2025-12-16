using Character.Components;
using UnityEngine;

public class WorldHpBarBinder
{
    private CharacterStats stats;
    private Transform followTarget;
    private WorldHpBar bar;
    private RectTransform rootRect;
    private WorldHpBarSystem owner;

    public WorldHpBar Bar => bar;

    public void Bind(CharacterStats stats, Transform followTarget, WorldHpBar bar, RectTransform rootRect, WorldHpBarSystem owner)
    {
        this.stats = stats;
        this.followTarget = followTarget;
        this.bar = bar;
        this.rootRect = rootRect;
        this.owner = owner;

        if (stats != null)
        {
            stats.OnHealthChanged += OnHpChanged;
            // 初始同步：绑定后立即刷新一次 UI，防止显示旧值
            OnHpChanged(stats.CurrentHP, stats.MaxHP.Value);
        }
    }

    public void Unbind()
    {
        if (stats != null)
        {
            stats.OnHealthChanged -= OnHpChanged;
            stats = null;
        }
        followTarget = null;
        // bar 的回收由 owner 负责
    }

    public void LateUpdate(Camera cam)
    {
        if (bar == null || followTarget == null || rootRect == null || cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(followTarget.position);
        // 若在相机后方则隐藏血条
        if (screenPos.z < 0f)
        {
            bar.gameObject.SetActive(false);
            return;
        }

        // 显示并将屏幕坐标转换为 rootRect 的本地坐标
        bar.gameObject.SetActive(true);
        Vector2 localPoint;
        // 选择用于 ScreenPoint->LocalPoint 转换的相机：优先使用 Canvas 的 worldCamera（若为 Overlay 则为 null）
        Camera canvasCam = null;
        if (owner != null) canvasCam = owner.GetCanvasCamera();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, new Vector2(screenPos.x, screenPos.y), canvasCam, out localPoint);
        if (bar.RectTransform != null)
            bar.RectTransform.anchoredPosition = localPoint;
    }

    private void OnHpChanged(float current, float max)
    {
        float p = (max > 0f) ? (current / max) : 0f;
        if (bar != null) bar.SetPercent(p);
    }
}