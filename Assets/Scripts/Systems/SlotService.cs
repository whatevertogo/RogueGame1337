using System;
using UnityEngine;
using RogueGame.Events;

/// <summary>
/// 订阅 ClearAllSlotsRequestedEvent 并执行槽位清理的集中服务
/// 将此组件放在场景中（例如 GameRoot 或 UI 根对象）以确保能在运行时接收事件
/// </summary>
public class SlotService : MonoBehaviour
{
    private void Awake()
    {
        EventBus.Subscribe<ClearAllSlotsRequestedEvent>(OnClearAllSlotsRequested);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<ClearAllSlotsRequestedEvent>(OnClearAllSlotsRequested);
    }

    private void OnClearAllSlotsRequested(ClearAllSlotsRequestedEvent evt)
    {
        Debug.Log("[SlotService] ClearAllSlotsRequestedEvent received, clearing slots");
        var slots = GameObject.FindObjectsOfType<CardSlot>();
        foreach (var slot in slots)
        {
            if (slot == null) continue;
            // 如果指定 PlayerId，可以根据需要过滤（当前 CardSlot 不包含 PlayerId 字段）
            slot.ClearSlot();
        }
    }
}
