using System;
using UnityEngine;

namespace Character.Projectiles
{
    /// <summary>
    /// 投射物生成器工具：
    /// - 优先从对象池获取投射物实例（若配置了 ProjectilePool），否则实例化预制体；
    /// - 根据给定方向计算旋转，以保证投射物朝向正确方向；
    /// - 统一调用 ProjectileBase.Init(...) 并在发生初始化错误时正确处理（归还至池或销毁）。
    /// </summary>
    public static class ProjectileSpawner
    {
        public static ProjectileBase Spawn(ProjectileConfig config, Vector3 pos, Vector2 dir, float damage, TeamType ownerTeam, Transform owner, LayerMask hitMask, StatusEffectDefinitionSO[] effects = null)
        {
            if (config == null)
            {
                CDTU.Utils.Logger.LogError("[ProjectileSpawner] ProjectileConfig 为 null，无法生成投射物。");
                return null;
            }

            if (config.projectilePrefab == null)
            {
                CDTU.Utils.Logger.LogError("[ProjectileSpawner] ProjectileConfig.projectilePrefab 为 null，无法生成投射物。");
                return null;
            }

            // 规范化方向并计算旋转
            Vector2 direction = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.up;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, direction);

            ProjectileBase projectile = null;
            bool usedPool = false;

            // 优先使用对象池获取投射物
            if (ProjectilePool.Instance != null)
            {
                try
                {
                    projectile = ProjectilePool.Instance.Get(config.projectilePrefab, pos, rot);
                    usedPool = projectile != null;
                }
                catch (Exception ex)
                {
                    CDTU.Utils.Logger.LogWarning($"[ProjectileSpawner] 从对象池获取投射物时发生异常：{ex.Message}");
                    projectile = null;
                    usedPool = false;
                }
            }

            // 如果对象池未返回实例，则实例化预制体
            if (projectile == null)
            {
                var go = GameObject.Instantiate(config.projectilePrefab, pos, rot);
                projectile = go.GetComponent<ProjectileBase>();
                if (projectile == null)
                {
                    CDTU.Utils.Logger.LogError($"[ProjectileSpawner] 预制体 {config.projectilePrefab.name} 缺少 ProjectileBase 组件。");
                    GameObject.Destroy(go);
                    return null;
                }
            }

            // 初始化投射物（若初始化失败，若来自池则归还，否则销毁）
            try
            {
                projectile.Init(config, direction, damage, ownerTeam, owner, hitMask, effects);
            }
            catch (Exception ex)
            {
                CDTU.Utils.Logger.LogError($"[ProjectileSpawner] 初始化投射物时出错：{ex.Message}");
                if (usedPool && ProjectilePool.Instance != null)
                {
                    try
                    {
                        ProjectilePool.Instance.Return(projectile);
                    }
                    catch { /* 忽略返回时异常 */ }
                }
                else
                {
                    GameObject.Destroy(projectile.gameObject);
                }
                return null;
            }

            return projectile;
        }
    }
}
