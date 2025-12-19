using UnityEngine;
using System.Collections.Generic;

namespace CardSystem.SkillSystem
{
    [CreateAssetMenu(fileName = "PlacementExecutor", menuName = "Card System/Executors/Placement")]
    /// <summary>
    /// 放置执行器：在指定位置放置Prefab物体
    /// </summary>
    public class PlacementExecutor : SkillExecutorSO
    {
        [Header("放置设置")]
        [Tooltip("要放置的Prefab")]
        public GameObject placementPrefab;
        
        [Tooltip("存活时间（秒），<=0表示永久存在")]
        public float lifetime = 10f;
        
        [Tooltip("是否使用地面法线旋转")]
        public bool useNormalRotation = true;
        
        [Tooltip("自定义旋转角度（欧拉角）")]
        public Vector3 customRotation = Vector3.zero;
        
        [Tooltip("缩放比例")]
        public Vector3 scale = Vector3.one;
        
        [Header("物理设置")]
        [Tooltip("是否启用物理")]
        public bool enablePhysics = false;
        
        [Tooltip("是否启用碰撞")]
        public bool enableCollision = true;
        
        [Header("数量限制")]
        [Tooltip("最大放置数量，-1表示无限制")]
        public int maxPlacements = 1;
        
        [Header("分组设置")]
        [Tooltip("放置物体的分组ID，用于管理")]
        public string groupId = "";
        
        public override void Execute(SkillDefinition skill, SkillContext ctx)
        {
            if (placementPrefab == null || ctx.AimPoint == null) return;
            
            // 获取放置管理器
            var placementManager = PlacementManager.Instance;
            if (placementManager == null)
            {
                Debug.LogWarning("[PlacementExecutor] PlacementManager未找到，请确保场景中有PlacementManager对象");
                return;
            }
            
            Vector3 position = ctx.AimPoint.Value;
            Quaternion rotation = Quaternion.identity;
            
            // 计算旋转
            if (useNormalRotation)
            {
                // 尝试获取地面法线
                RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, 1f);
                if (hit.collider != null)
                {
                    // 使用法线旋转
                    rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                }
                else
                {
                    rotation = Quaternion.Euler(customRotation);
                }
            }
            else
            {
                rotation = Quaternion.Euler(customRotation);
            }
            
            // 放置物体
            GameObject placedObject = placementManager.PlaceObject(
                placementPrefab, 
                position, 
                rotation, 
                scale, 
                groupId,
                maxPlacements
            );
            
            // 配置物理属性
            if (placedObject != null)
            {
                // 配置碰撞体
                Collider2D[] colliders = placedObject.GetComponentsInChildren<Collider2D>();
                foreach (var collider in colliders)
                {
                    collider.enabled = enableCollision;
                }
                
                // 配置刚体
                Rigidbody2D rb = placedObject.GetComponentInChildren<Rigidbody2D>();
                if (rb != null)
                {
                    rb.isKinematic = !enablePhysics;
                }
                
                // 设置存活时间
                if (lifetime > 0)
                {
                    placementManager.SetLifetime(placedObject, lifetime);
                }
            }
        }
    }
}