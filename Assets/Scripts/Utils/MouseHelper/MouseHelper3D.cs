/*
 * =========================
 *  MouseHelper3D
 *  适用：3D / 斜视角 / 等距视角
 * =========================
 */
using UnityEngine;

public static class MouseHelper3D
{
    private static Camera Cam =>  Camera.main;

    /// <summary>
    /// 获取鼠标射线
    /// </summary>
    public static Ray GetMouseRay()
    {
        Vector2 screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        return Cam.ScreenPointToRay(screenPos);
    }

    /// <summary>
    /// 射线检测到指定 Layer 的物体
    /// </summary>
    public static bool RaycastObject(
        out RaycastHit hit,
        float maxDistance = 1000f,
        LayerMask mask = default
    )
    {
        Ray ray = GetMouseRay();
        return Physics.Raycast(ray, out hit, maxDistance, mask);
    }

    /// <summary>
    /// 射线检测地面（常用于点击移动）
    /// </summary>
    public static bool RaycastToGround(
        out Vector3 hitPoint,
        float maxDistance = 1000f,
        LayerMask groundMask = default
    )
    {
        Ray ray = GetMouseRay();

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, groundMask))
        {
            hitPoint = hit.point;
            return true;
        }

        hitPoint = Vector3.zero;
        return false;
    }

    /// <summary>
    /// 射线投射到指定平面（不依赖碰撞体）
    /// </summary>
    public static bool RaycastToPlane(
        Plane plane,
        out Vector3 hitPoint
    )
    {
        Ray ray = GetMouseRay();

        if (plane.Raycast(ray, out float distance))
        {
            hitPoint = ray.GetPoint(distance);
            return true;
        }

        hitPoint = Vector3.zero;
        return false;
    }

    /// <summary>
    /// 从原点指向目标点的方向（忽略 Y）
    /// </summary>
    public static Vector3 GetFlatDirection(Vector3 origin, Vector3 target)
    {
        Vector3 dir = target - origin;
        dir.y = 0;
        return dir.normalized;
    }
}
