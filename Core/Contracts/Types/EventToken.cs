using System;

namespace Maple.Core
{
    /// <summary>
    /// 订阅句柄，Dispose 即取消订阅。
    /// 在 MonoBehaviour.OnDisable/OnDestroy 中调用 Dispose() 防止泄漏。
    /// </summary>
    public readonly struct EventToken : IDisposable
    {
        private readonly Action _dispose;

        public EventToken(Action dispose) => _dispose = dispose;

        public void Dispose() => _dispose?.Invoke();
    }
}
