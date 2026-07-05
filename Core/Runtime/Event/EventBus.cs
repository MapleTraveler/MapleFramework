using System;
using System.Collections.Generic;

namespace Maple.Core
{
    /// <summary>
    /// 按事件类型维护委托列表的事件总线。不使用反射，零配置。
    /// 建议事件类型使用 struct 以减少 GC。
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, object> _handlers = new();

        public EventToken Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var list = GetOrCreateList<T>();
            list.Add(handler);
            return new EventToken(() => list.Remove(handler));
        }

        public void Publish<T>(T evt)
        {
            if (!_handlers.TryGetValue(typeof(T), out var obj) || obj is not List<Action<T>> list)
                return;

            // 快照遍历，避免回调中增删导致枚举异常
            var snapshot = Pooler.IsReady
                ? Pooler.Get<List<Action<T>>>(l => l.Clear())
                : new List<Action<T>>(list.Count);

            snapshot.AddRange(list);

            foreach (var handler in snapshot)
            {
                try
                {
                    handler?.Invoke(evt);
                }
                catch (Exception e)
                {
                    GLogger.LogException(e, LogTag.EVENT, $"Publish<{typeof(T).Name}> handler threw");
                }
            }

            if (Pooler.IsReady)
                Pooler.Release(snapshot);
        }

        private List<Action<T>> GetOrCreateList<T>()
        {
            if (_handlers.TryGetValue(typeof(T), out var obj))
                return (List<Action<T>>)obj;

            var list = new List<Action<T>>(8);
            _handlers[typeof(T)] = list;
            return list;
        }
    }
}
