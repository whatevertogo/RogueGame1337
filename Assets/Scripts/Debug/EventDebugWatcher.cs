using UnityEngine;
using System;
using System.Collections.Generic;
using RogueGame.Map;
using System.Linq;
using System.Reflection;

// 事件调试观察器：在编辑器运行时订阅关键事件并记录时间戳与信息，便于验证事件顺序
[DisallowMultipleComponent]
public class EventDebugWatcher : MonoBehaviour
{
    [Header("Debug 设置")]
    [SerializeField] private bool enable = true;
    [SerializeField] private int maxLogs = 200;
    [SerializeField] private bool logToConsole = true;

    private readonly List<string> _logs = new List<string>();

    private readonly Dictionary<string, Delegate> _subDelegates = new();

    private void OnEnable()
    {
        if (!enable) return;

        SubscribeByName("RoomEnteredEvent");
        SubscribeByName("RoomClearedEvent");
        SubscribeByName("CombatStartedEvent");
        SubscribeByName("StartRunRequestedEvent");
        SubscribeByName("DoorEnterRequestedEvent");

        AddLog("EventDebugWatcher enabled");
    }

    private void OnDisable()
    {
        if (!enable) return;

        UnsubscribeByName("RoomEnteredEvent");
        UnsubscribeByName("RoomClearedEvent");
        UnsubscribeByName("CombatStartedEvent");
        UnsubscribeByName("StartRunRequestedEvent");
        UnsubscribeByName("DoorEnterRequestedEvent");

        AddLog("EventDebugWatcher disabled");
    }

    private void SubscribeByName(string typeName)
    {
        try
        {
            var evtType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
                .FirstOrDefault(t => t.Name == typeName);
            if (evtType == null)
            {
                AddLog($"SubscribeByName: type {typeName} not found");
                return;
            }

            var subscribeMethod = typeof(EventBus).GetMethod("Subscribe");
            var generic = subscribeMethod.MakeGenericMethod(evtType);

            // Build Action<T> that calls OnEventReceived(string typeName, object evt)
            var param = System.Linq.Expressions.Expression.Parameter(evtType, "e");
            var convert = System.Linq.Expressions.Expression.Convert(param, typeof(object));
            var instance = System.Linq.Expressions.Expression.Constant(this);
            var method = typeof(EventDebugWatcher).GetMethod(nameof(OnEventReceived), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var typeNameConst = System.Linq.Expressions.Expression.Constant(typeName);
            var call = System.Linq.Expressions.Expression.Call(instance, method, typeNameConst, convert);
            var lambdaType = typeof(Action<>).MakeGenericType(evtType);
            var lambda = System.Linq.Expressions.Expression.Lambda(lambdaType, call, param).Compile();

            // store delegate for unsubscribing later
            _subDelegates[typeName] = (Delegate)lambda;

            generic.Invoke(null, new object[] { lambda });
            AddLog($"Subscribed to {typeName}");
        }
        catch (Exception ex)
        {
            AddLog($"SubscribeByName error for {typeName}: {ex.Message}");
        }
    }

    private void UnsubscribeByName(string typeName)
    {
        try
        {
            if (!_subDelegates.TryGetValue(typeName, out var del)) return;

            var evtType = del.GetType().GetGenericArguments()[0];
            var unsubscribeMethod = typeof(EventBus).GetMethod("Unsubscribe");
            var generic = unsubscribeMethod.MakeGenericMethod(evtType);
            generic.Invoke(null, new object[] { del });
            _subDelegates.Remove(typeName);
            AddLog($"Unsubscribed from {typeName}");
        }
        catch (Exception ex)
        {
            AddLog($"UnsubscribeByName error for {typeName}: {ex.Message}");
        }
    }

    private void OnEventReceived(string typeName, object evt)
    {
        var details = FormatEventDetails(evt);
        AddLog($"{typeName} @ {Time.realtimeSinceStartup:F3}s | {details}");
    }

    private string FormatEventDetails(object evt)
    {
        if (evt == null) return "(null)";
        try
        {
            var t = evt.GetType();
            var parts = new List<string> { t.Name };
            foreach (var f in t.GetFields())
            {
                try
                {
                    var v = f.GetValue(evt);
                    if (v == null) continue;
                    if (f.FieldType.IsPrimitive || f.FieldType == typeof(string))
                        parts.Add($"{f.Name}={v}");
                    else if (f.FieldType.Name == "RoomType")
                        parts.Add($"{f.Name}={v}");
                    else if (f.FieldType.Name == "Direction")
                        parts.Add($"{f.Name}={v}");
                    else if (f.Name == "StartMeta")
                    {
                        var metaName = v.GetType().GetField("BundleName")?.GetValue(v) ?? "(meta)";
                        parts.Add($"StartMeta={metaName}");
                    }
                }
                catch { }
            }
            return string.Join(" ", parts);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private void AddLog(string s)
    {
        var line = $"[{DateTime.UtcNow:HH:mm:ss.fff}] {s}";
        _logs.Add(line);
        if (logToConsole) Debug.Log(line);
        TrimLogs();
    }

    private void TrimLogs()
    {
        if (_logs.Count <= maxLogs) return;
        int remove = _logs.Count - maxLogs;
        _logs.RemoveRange(0, remove);
    }

    // 简单的 OnGUI 展示，方便在编辑器里实时查看（非生产界面）
    private Vector2 _scroll;
    private void OnGUI()
    {
        if (!enable) return;

        GUILayout.BeginArea(new Rect(8, 8, 520, 320), GUI.skin.box);
        var headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        GUILayout.Label("Event Debug Watcher", headerStyle);
        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Width(500), GUILayout.Height(280));
        foreach (var l in _logs)
        {
            GUILayout.Label(l);
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
