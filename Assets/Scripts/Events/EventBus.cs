using System;
using System.Collections.Generic;
using System.Linq;

#if UNITASK
using Cysharp.Threading.Tasks;
#endif

namespace Core.Events
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, HashSet<Delegate>> _listeners = new();

        public static void Subscribe<TEvent>(Action<TEvent> callback)
        {
            var type = typeof(TEvent);
            if (!_listeners.TryGetValue(type, out var set))
                _listeners[type] = set = new HashSet<Delegate>();
            set.Add(callback);
        }

#if UNITASK
        public static void Subscribe<TEvent>(Func<TEvent, UniTaskVoid> handler)
        {
            var type = typeof(TEvent);
            if (!_listeners.TryGetValue(type, out var set))
                _listeners[type] = set = new HashSet<Delegate>();
            set.Add(handler);
        }
#endif

        public static void Publish<T>(T evt)
        {
            if (!_listeners.TryGetValue(typeof(T), out var set)) return;
            foreach (var d in set.ToArray())
            {
#if UNITASK
                if (d is Func<T, UniTaskVoid> asyncHandler)
                {
                    _ = asyncHandler(evt); // fire-and-forget
                    continue;
                }
#endif
                if (d is Action<T> Handler)
                    Handler(evt);
            }
        }

        public static void Unsubscribe<T>(Action<T> callback)
        {
            if (_listeners.TryGetValue(typeof(T), out var set))
            {
                set.Remove(callback);
                if (set.Count == 0) _listeners.Remove(typeof(T));
            }
        }

#if UNITASK
        public static void Unsubscribe<T>(Func<T, UniTaskVoid> handler)
        {
            if (_listeners.TryGetValue(typeof(T), out var set))
            {
                set.Remove(handler);
                if (set.Count == 0) _listeners.Remove(typeof(T));
            }
        }
#endif
    }
}
