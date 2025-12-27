using System;
using System.Collections.Generic;
using System.Linq;

public static class EventBus
{
    private static readonly Dictionary<Type, HashSet<Delegate>> _listeners
        = new Dictionary<Type, HashSet<Delegate>>();

    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="callback"></param>
    public static void Subscribe<T>(Action<T> callback)
    {
        var type = typeof(T);
        if (!_listeners.TryGetValue(type, out var set))
        {
            set = new HashSet<Delegate>();
            _listeners[type] = set;
        }
        set.Add(callback); // HashSet 自动去重
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="callback"></param>
    public static void Unsubscribe<T>(Action<T> callback)
    {
        var type = typeof(T);
        if (_listeners.TryGetValue(type, out var set))
        {
            set.Remove(callback);
            if (set.Count == 0)
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
        if (evt == null)
        {
            CDTU.Utils.CDLogger.LogWarning($"[EventBus] Publish: Event of type {typeof(T).Name} is null");
            return;
        }
        var type = typeof(T);
        if (!_listeners.TryGetValue(type, out var set))
            return;

        // 创建副本以防在回调中修改订阅列表
        var snapshot = set.ToArray();
        foreach (var d in snapshot)
        {
            try
            {
                ((Action<T>)d)?.Invoke(evt);
            }
            catch (Exception ex)
            {
                CDTU.Utils.CDLogger.LogError($"[EventBus] Publish: Exception in event handler for {typeof(T).Name}: {ex}");
            }
        }
    }


    public static void Clear()
    {
        _listeners.Clear();
    }
}
