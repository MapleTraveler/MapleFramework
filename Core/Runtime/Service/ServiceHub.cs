using System;
using System.Collections.Generic;

namespace Maple.Core
{
    /// <summary>
    /// 轻量服务中心：内置 EventBus / PoolProvider 的初始化，
    /// 同时提供 Register / Get 泛型服务定位能力。
    /// </summary>
    public static class ServiceHub
    {
        private static readonly Dictionary<Type, object> _services = new();
        private static bool _initialized;

        public static bool IsReady => _initialized;

        /// <summary>
        /// 框架启动入口，初始化内置核心服务。
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            var eventBus = new EventBus();
            Eventer.Initialize(eventBus);
            Register<IEventBus>(eventBus);

            var poolProvider = new PoolProvider();
            Pooler.Initialize(poolProvider);
            Register<IPoolProvider>(poolProvider);

            Register<IConfigProvider>(new ConfigProvider());

            _initialized = true;
            GLogger.LogInfo(LogTag.FRAMEWORK, "ServiceHub initialized.");
        }

        /// <summary>
        /// 注册服务实例，以接口类型为 key。
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            _services[typeof(T)] = service;
        }

        /// <summary>
        /// 注销指定接口类型的服务，仅当当前注册实例与 <paramref name="expectedInstance"/> 相同时移除。
        /// 未注册或当前持有者不是期望实例时返回 false。
        /// </summary>
        public static bool Unregister<T>(T expectedInstance) where T : class
        {
            if (expectedInstance == null)
                return false;

            if (_services.TryGetValue(typeof(T), out var current) &&
                ReferenceEquals(current, expectedInstance))
            {
                _services.Remove(typeof(T));
                return true;
            }

            return false;
        }

        /// <summary>
        /// 按接口类型注销当前注册的服务，不验证实例所有权。
        /// 仅用于组合根关闭或确认无并发注册者的场景；业务侧优先使用 Unregister&lt;T&gt;(T expectedInstance)。
        /// 未注册时安全返回 false。
        /// </summary>
        public static bool Unregister<T>() where T : class
        {
            return _services.Remove(typeof(T));
        }

        /// <summary>
        /// 获取已注册的服务，未找到返回 null。
        /// </summary>
        public static T Get<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var service) ? (T)service : null;
        }

        /// <summary>
        /// 获取已注册的服务，未找到则抛异常。
        /// </summary>
        public static T Require<T>() where T : class
        {
            return Get<T>() ?? throw new InvalidOperationException(
                $"服务 {typeof(T).Name} 未注册，请在 ServiceHub.Initialize() 之后调用 Register<{typeof(T).Name}>()");
        }

        /// <summary>
        /// 关闭所有服务，释放资源。
        /// </summary>
        public static void Shutdown()
        {
            Pooler.Shutdown();
            Eventer.Shutdown();
            _services.Clear();
            _initialized = false;
            GLogger.LogInfo(LogTag.FRAMEWORK, "ServiceHub shutdown.");
        }
    }
}
