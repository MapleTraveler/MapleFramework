using System;

namespace Maple.Core
{
    public interface IPoolProvider
    {
        T Get<T>(Action<T> onGet = null) where T : class, new();
        void Release<T>(T obj) where T : class, new();
        void ClearAllPools();
        void ReleasePool<T>() where T : class;
    }
}
