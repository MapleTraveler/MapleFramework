using System.Collections.Generic;
using UnityEngine;

namespace Maple.Core
{
    /// <summary>
    /// GameObject 专用对象池，管理 Prefab 的实例化与回收。
    /// </summary>
    public sealed class GameObjectPool : IObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Stack<GameObject> _stack = new();

        public int CountInactive => _stack.Count;

        public GameObjectPool(GameObject prefab, Transform parent = null, int prewarm = 0)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < prewarm; i++)
            {
                var go = Object.Instantiate(_prefab, _parent);
                go.SetActive(false);
                _stack.Push(go);
            }
        }

        public GameObject Get(Vector3 position = default, Quaternion rotation = default)
        {
            GameObject go = _stack.Count > 0 ? _stack.Pop() : Object.Instantiate(_prefab, _parent);
            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);
            return go;
        }

        public void Return(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            if (_parent != null) go.transform.SetParent(_parent);
            _stack.Push(go);
        }

        public void Clear()
        {
            while (_stack.Count > 0)
            {
                var go = _stack.Pop();
                if (go != null) Object.Destroy(go);
            }
        }
    }
}
