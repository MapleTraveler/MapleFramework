using System;
using System.Diagnostics;

namespace Maple.Core
{
    /// <summary>
    /// 对象池的静态门面，提供全局快捷访问。
    /// </summary>
    public static class Pooler
    {
        private static IPoolProvider _provider;

        public static bool IsReady => _provider != null;

        public static T Get<T>(Action<T> onGet = null) where T : class, new()
        {
            AssertReady();
            return _provider.Get(onGet);
        }

        public static void Release<T>(T obj) where T : class, new()
        {
            AssertReady();
            _provider.Release(obj);
        }

        [Conditional("DEBUG")]
        private static void AssertReady()
        {
            if (_provider == null)
                throw new InvalidOperationException("Pooler 未初始化，请先调用 ServiceHub.Initialize()");
        }

        internal static void Initialize(IPoolProvider provider) => _provider = provider;
        internal static void Shutdown() => _provider = null;
    }
}
