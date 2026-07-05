using System;
using System.Collections.Generic;

namespace Maple.Core
{
    /// <summary>
    /// 泛型对象池。支持预热、容量上限、IPoolable 回调。
    /// </summary>
    public sealed class ObjectPool<T> : IObjectPool<T> where T : class, new()
    {
        private readonly Stack<T> _stack;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;
        private readonly int _maxSize;

        public int CountInactive => _stack.Count;

        public ObjectPool(
            Action<T> onReturn = null,
            Action<T> onGet = null,
            int prewarm = 0,
            int maxSize = 1024)
        {
            _stack = new Stack<T>(prewarm > 0 ? prewarm : 8);
            _onGet = onGet;
            _onReturn = onReturn;
            _maxSize = maxSize;

            for (int i = 0; i < prewarm; i++)
                _stack.Push(new T());
        }

        public T Get()
        {
            T obj = _stack.Count > 0 ? _stack.Pop() : new T();

            if (obj is IPoolable poolable)
                poolable.OnSpawn();

            _onGet?.Invoke(obj);
            return obj;
        }

        public void Return(T obj)
        {
            if (obj == null) return;

            if (obj is IPoolable poolable)
                poolable.OnDespawn();

            _onReturn?.Invoke(obj);

            if (_stack.Count < _maxSize)
                _stack.Push(obj);
        }

        public void Clear() => _stack.Clear();
    }
}
