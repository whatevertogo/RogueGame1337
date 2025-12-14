using System;
using System.Collections.Generic;

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
        var type = typeof(T);
        if (_listeners.TryGetValue(type, out var list))
        {
            // 防止回调中修改列表
            var snapshot = list.ToArray();
            foreach (var cb in snapshot)
            {
                ((Action<T>)cb)?.Invoke(evt);
            }
        }
    }

    public static void Clear()
    {
        _listeners.Clear();
    }
}
