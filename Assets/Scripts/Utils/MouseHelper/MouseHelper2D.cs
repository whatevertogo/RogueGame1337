using UnityEngine;
using UnityEngine.InputSystem;

/*
 * =========================
 *  MouseHelper2D
 *  适用：2D / 2.5D / 正交相机
 *  约定：交互平面 Z = 0
 * =========================
 */
public static class MouseHelper2D
{
    private static Camera _cam;
    private static Camera Cam => _cam ??= Camera.main;

    private static Vector2 _lastWorldPos;
    private static bool _hasLast;

    /// <summary>
    /// 获取鼠标在世界空间的位置（Z=0）
    /// </summary>
    public static Vector3 GetWorldPosition()
    {
        Vector3 screenPos = Mouse.current.position.ReadValue();
        screenPos.z = -Cam.transform.position.z;

        Vector3 worldPos = Cam.ScreenToWorldPoint(screenPos);
        worldPos.z = 0;
        return worldPos;
    }

    /// <summary>
    /// 获取鼠标世界坐标（Vector2 版本）
    /// </summary>
    public static Vector2 GetWorldPosition2D()
    {
        Vector3 pos = GetWorldPosition();
        return new Vector2(pos.x, pos.y);
    }

    /// <summary>
    /// 从某个世界点指向鼠标的方向（单位向量）
    /// </summary>
    public static Vector2 GetDirectionFrom(Vector2 origin)
    {
        Vector2 target = GetWorldPosition2D();
        return (target - origin).normalized;
    }

    /// <summary>
    /// 判断鼠标是否在指定世界矩形内
    /// </summary>
    public static bool IsMouseInRect(Rect worldRect)
    {
        Vector2 mousePos = GetWorldPosition2D();
        return worldRect.Contains(mousePos);
    }

    /// <summary>
    /// 获取鼠标拖拽的世界位移（帧差）
    /// </summary>
    public static Vector2 GetDragDelta()
    {
        Vector2 current = GetWorldPosition2D();

        if (!_hasLast)
        {
            _lastWorldPos = current;
            _hasLast = true;
            return Vector2.zero;
        }

        Vector2 delta = current - _lastWorldPos;
        _lastWorldPos = current;
        return delta;
    }

    /// <summary>
    /// 重置拖拽状态（如 MouseDown 时调用）
    /// </summary>
    public static void ResetDrag()
    {
        _hasLast = false;
    }
}

