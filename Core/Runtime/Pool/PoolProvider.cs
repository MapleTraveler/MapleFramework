using System;
using System.Collections.Generic;

namespace Maple.Core
{
    /// <summary>
    /// 通用对象池管理器，按类型自动创建和管理 ObjectPool。
    /// </summary>
    public sealed class PoolProvider : IPoolProvider
    {
        private readonly Dictionary<Type, IObjectPool> _pools = new();

        public T Get<T>(Action<T> onGet = null) where T : class, new()
        {
            return GetOrCreatePool<T>(onGet).Get();
        }

        public void Release<T>(T obj) where T : class, new()
        {
            if (obj == null) return;
            if (_pools.TryGetValue(typeof(T), out var pool))
                ((ObjectPool<T>)pool).Return(obj);
        }

        public void ClearAllPools()
        {
            foreach (var pool in _pools.Values)
                pool.Clear();
        }

        public void ReleasePool<T>() where T : class
        {
            var type = typeof(T);
            if (_pools.Remove(type, out var pool))
                pool.Clear();
        }

        private ObjectPool<T> GetOrCreatePool<T>(Action<T> onGet = null) where T : class, new()
        {
            var type = typeof(T);
            if (_pools.TryGetValue(type, out var pool))
                return (ObjectPool<T>)pool;

            var newPool = new ObjectPool<T>(onGet: onGet);
            _pools[type] = newPool;
            return newPool;
        }
    }
}
