using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CDTU.Utils;
using Core.Events;
using RogueGame.Events;

/// <summary>
/// 放置物体管理器：管理所有通过技能放置的物体
/// </summary>
public class PlacementManager : Singleton<PlacementManager>
{
    [Serializable]
    private class PlacementInfo
    {
        public GameObject gameObject;
        public string groupId;
        public float createTime;
        public float lifetime;
        
        public PlacementInfo(GameObject obj, string group, float life)
        {
            gameObject = obj;
            groupId = group;
            lifetime = life;
            createTime = Time.time;
        }
    }
    
    // 所有放置的物体
    private List<PlacementInfo> placements = new List<PlacementInfo>();
    
    // 按分组存储的物体
    private Dictionary<string, List<PlacementInfo>> placementGroups = new Dictionary<string, List<PlacementInfo>>();
    
    protected override void Awake()
    {
        base.Awake();
        // 订阅房间切换事件，用于清理物体
        // EventBus.Subscribe<RoomExitedEvent>(OnRoomExited);
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        // EventBus.Unsubscribe<RoomExitedEvent>(OnRoomExited);
    }
    
    /// <summary>
    /// 放置物体
    /// </summary>
    /// <param name="prefab">要放置的Prefab</param>
    /// <param name="position">放置位置</param>
    /// <param name="rotation">放置旋转</param>
    /// <param name="scale">放置缩放</param>
    /// <param name="groupId">分组ID</param>
    /// <param name="maxPlacements">该分组的最大放置数量</param>
    /// <returns>放置的物体实例</returns>
    public GameObject PlaceObject(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale, string groupId = "", int maxPlacements = -1)
    {
        if (prefab == null) return null;
        
        // 检查数量限制
        if (!string.IsNullOrEmpty(groupId) && maxPlacements > 0)
        {
            if (placementGroups.ContainsKey(groupId) && placementGroups[groupId].Count >= maxPlacements)
            {
                // 移除最早的物体
                RemoveOldestPlacementInGroup(groupId);
            }
        }
        
        // 实例化物体
        GameObject placedObject = Instantiate(prefab, position, rotation);
        placedObject.transform.localScale = scale;
        
        // 创建放置信息
        PlacementInfo info = new PlacementInfo(placedObject, groupId, -1f);
        placements.Add(info);
        
        // 添加到分组
        if (!string.IsNullOrEmpty(groupId))
        {
            if (!placementGroups.ContainsKey(groupId))
            {
                placementGroups[groupId] = new List<PlacementInfo>();
            }
            placementGroups[groupId].Add(info);
        }
        
        return placedObject;
    }
    
    /// <summary>
    /// 设置物体的存活时间
    /// </summary>
    /// <param name="obj">要设置的物体</param>
    /// <param name="lifetime">存活时间（秒）</param>
    public void SetLifetime(GameObject obj, float lifetime)
    {
        if (obj == null || lifetime <= 0) return;
        
        PlacementInfo info = placements.Find(p => p.gameObject == obj);
        if (info != null)
        {
            info.lifetime = lifetime;
            info.createTime = Time.time;
            // 启动销毁协程
            StartCoroutine(DestroyAfterDelay(obj, lifetime));
        }
    }
    
    /// <summary>
    /// 延迟销毁物体
    /// </summary>
    private IEnumerator DestroyAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (obj != null)
        {
            RemovePlacement(obj);
            Destroy(obj);
        }
    }
    
    /// <summary>
    /// 移除分组中最旧的放置物体
    /// </summary>
    private void RemoveOldestPlacementInGroup(string groupId)
    {
        if (string.IsNullOrEmpty(groupId) || !placementGroups.ContainsKey(groupId)) return;
        
        List<PlacementInfo> groupPlacements = placementGroups[groupId];
        if (groupPlacements.Count == 0) return;
        
        // 找到最旧的物体
        PlacementInfo oldest = groupPlacements[0];
        foreach (var placement in groupPlacements)
        {
            if (placement.createTime < oldest.createTime)
            {
                oldest = placement;
            }
        }
        
        // 移除物体
        RemovePlacement(oldest.gameObject);
        if (oldest.gameObject != null)
        {
            Destroy(oldest.gameObject);
        }
    }
    
    /// <summary>
    /// 移除放置物体的信息
    /// </summary>
    private void RemovePlacement(GameObject obj)
    {
        if (obj == null) return;
        
        PlacementInfo info = placements.Find(p => p.gameObject == obj);
        if (info != null)
        {
            // 从总列表中移除
            placements.Remove(info);
            
            // 从分组中移除
            if (!string.IsNullOrEmpty(info.groupId) && placementGroups.ContainsKey(info.groupId))
            {
                placementGroups[info.groupId].Remove(info);
            }
        }
    }
    
    /// <summary>
    /// 清理指定分组的所有物体
    /// </summary>
    public void ClearGroup(string groupId)
    {
        if (string.IsNullOrEmpty(groupId) || !placementGroups.ContainsKey(groupId)) return;
        
        List<PlacementInfo> groupPlacements = new List<PlacementInfo>(placementGroups[groupId]);
        foreach (var placement in groupPlacements)
        {
            if (placement.gameObject != null)
            {
                Destroy(placement.gameObject);
            }
            placements.Remove(placement);
        }
        
        placementGroups[groupId].Clear();
    }
    
    /// <summary>
    /// 清理所有物体
    /// </summary>
    public void ClearAll()
    {
        foreach (var placement in placements)
        {
            if (placement.gameObject != null)
            {
                Destroy(placement.gameObject);
            }
        }
        
        placements.Clear();
        placementGroups.Clear();
    }
    
    /// <summary>
    /// 获取指定分组的物体数量
    /// </summary>
    public int GetGroupPlacementCount(string groupId)
    {
        if (string.IsNullOrEmpty(groupId) || !placementGroups.ContainsKey(groupId))
            return 0;
            
        return placementGroups[groupId].Count;
    }
    
    /// <summary>
    /// 房间退出时的处理
    /// </summary>
    private void OnRoomExited(object evt)
    {
        // 清理所有放置的物体
        ClearAll();
    }
}