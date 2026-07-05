using System;
using System.Diagnostics;

namespace Maple.Core
{
    /// <summary>
    /// 事件总线的静态门面，提供全局快捷访问。
    /// 在不方便构造注入的地方（MonoBehaviour 等）使用。
    /// </summary>
    public static class Eventer
    {
        private static IEventBus _bus;

        public static bool IsReady => _bus != null;

        public static EventToken Subscribe<T>(Action<T> handler)
        {
            AssertReady();
            return _bus.Subscribe(handler);
        }

        public static void Publish<T>(T evt)
        {
            AssertReady();
            _bus.Publish(evt);
        }

        [Conditional("DEBUG")]
        private static void AssertReady()
        {
            if (_bus == null)
                throw new InvalidOperationException("Eventer 未初始化，请先调用 ServiceHub.Initialize()");
        }

        internal static void Initialize(IEventBus bus) => _bus = bus;
        internal static void Shutdown() => _bus = null;
    }
}
