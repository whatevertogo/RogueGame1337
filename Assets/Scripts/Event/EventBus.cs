using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _listeners
        = new Dictionary<Type, List<Delegate>>();

    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="callback"></param>
    public static void Subscribe<T>(Action<T> callback)
    {
        var type = typeof(T);
        if (!_listeners.TryGetValue(type, out var list))
        {
            list = new List<Delegate>();
            _listeners[type] = list;
        }

        if (!list.Contains(callback))
            list.Add(callback);
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="callback"></param>
    public static void Unsubscribe<T>(Action<T> callback)
    {
        var type = typeof(T);
        if (_listeners.TryGetValue(type, out var list))
        {
            list.Remove(callback);
            if (list.Count == 0)
                _listeners.Remove(type);
        }
    }

    /// <summary>
    /// 发布事件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="evt"></param>
    public static void Publish<T>(T evt)
    {
        if (evt == null) {
            Debug.LogWarning($"[EventBus] Publish: Event of type {typeof(T).Name} is null");
            return;
        }

        var type = typeof(T);
        if (!_listeners.TryGetValue(type, out var list))
            return; // 没有监听器是正常情况

        // 防止回调中修改列表
        var snapshot = list.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
        {
            try {
                ((Action<T>)snapshot[i])?.Invoke(evt);
            } catch (System.Exception ex) {
                Debug.LogError($"[EventBus] 事件处理失败 (事件类型: {type.Name}, 索引: {i}): {ex.Message}");
            }
        }
    }

    public static void Clear()
    {
        _listeners.Clear();
    }
}
