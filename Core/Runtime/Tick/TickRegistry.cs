using System;
using System.Collections.Generic;

namespace Maple.Core
{
    /// <summary>
    /// ITickRegistry 的默认实现：不依赖 MonoBehaviour 的纯 C# 派发器。
    /// 由宿主（GameRoot）在 Update / FixedUpdate 中调用 Tick / FixedTick 驱动。
    ///
    /// 派发语义是接口契约，见 <see cref="ITickRegistry"/> 注释。这里只说实现手法：
    /// 派发前把注册表拷进复用缓冲区并遍历缓冲区，注册表本身不参与迭代，
    /// 因此派发过程中的增删不会打乱索引，也不必延迟到帧末执行；
    /// 帧内被注销的实例记入 _removed 集合，遍历到时跳过，从而"注销立即生效"。
    /// 缓冲区与集合都是复用字段，预热后稳定状态下无堆分配。
    /// </summary>
    public sealed class TickRegistry : ITickRegistry
    {
        private readonly List<ITickable> _tickables = new();
        private readonly List<IFixedTickable> _fixedTickables = new();

        private readonly List<ITickable> _tickBuffer = new();
        private readonly List<IFixedTickable> _fixedTickBuffer = new();

        private readonly HashSet<ITickable> _removedTickables = new();
        private readonly HashSet<IFixedTickable> _removedFixedTickables = new();

        private bool _ticking;
        private bool _fixedTicking;

        /// <summary> 当前注册的 ITickable 数量。 </summary>
        public int TickableCount => _tickables.Count;

        /// <summary> 当前注册的 IFixedTickable 数量。 </summary>
        public int FixedTickableCount => _fixedTickables.Count;

        // ──────────────────────────────────────────────────────────────────────
        // ITickRegistry
        // ──────────────────────────────────────────────────────────────────────

        public void Register(ITickable tickable)
        {
            if (tickable != null && !_tickables.Contains(tickable))
                _tickables.Add(tickable);
        }

        public void Unregister(ITickable tickable)
        {
            if (tickable == null)
                return;

            if (_tickables.Remove(tickable) && _ticking)
                _removedTickables.Add(tickable);
        }

        public void Register(IFixedTickable fixedTickable)
        {
            if (fixedTickable != null && !_fixedTickables.Contains(fixedTickable))
                _fixedTickables.Add(fixedTickable);
        }

        public void Unregister(IFixedTickable fixedTickable)
        {
            if (fixedTickable == null)
                return;

            if (_fixedTickables.Remove(fixedTickable) && _fixedTicking)
                _removedFixedTickables.Add(fixedTickable);
        }

        // ──────────────────────────────────────────────────────────────────────
        // 派发
        // ──────────────────────────────────────────────────────────────────────

        /// <summary> 派发一轮 Tick。由宿主在 Update 中调用。 </summary>
        public void Tick(float deltaTime)
        {
            if (_ticking)
            {
                GLogger.LogError(LogTag.FRAMEWORK,
                    "TickRegistry.Tick 不支持重入，本次调用已忽略。请检查是否在某个 ITickable.Tick 内部又驱动了注册表。");
                return;
            }

            if (_tickables.Count == 0)
                return;

            _ticking = true;
            try
            {
                _tickBuffer.AddRange(_tickables);

                for (int i = 0; i < _tickBuffer.Count; i++)
                {
                    ITickable tickable = _tickBuffer[i];
                    if (_removedTickables.Count > 0 && _removedTickables.Contains(tickable))
                        continue;

                    try
                    {
                        tickable.Tick(deltaTime);
                    }
                    catch (Exception e)
                    {
                        GLogger.LogException(e, LogTag.FRAMEWORK,
                            $"TickRegistry.Tick: {tickable.GetType().Name} threw");
                    }
                }
            }
            finally
            {
                _tickBuffer.Clear();
                _removedTickables.Clear();
                _ticking = false;
            }
        }

        /// <summary> 派发一轮 FixedTick。由宿主在 FixedUpdate 中调用。 </summary>
        public void FixedTick(float fixedDeltaTime)
        {
            if (_fixedTicking)
            {
                GLogger.LogError(LogTag.FRAMEWORK,
                    "TickRegistry.FixedTick 不支持重入，本次调用已忽略。请检查是否在某个 IFixedTickable.FixedTick 内部又驱动了注册表。");
                return;
            }

            if (_fixedTickables.Count == 0)
                return;

            _fixedTicking = true;
            try
            {
                _fixedTickBuffer.AddRange(_fixedTickables);

                for (int i = 0; i < _fixedTickBuffer.Count; i++)
                {
                    IFixedTickable fixedTickable = _fixedTickBuffer[i];
                    if (_removedFixedTickables.Count > 0 &&
                        _removedFixedTickables.Contains(fixedTickable))
                    {
                        continue;
                    }

                    try
                    {
                        fixedTickable.FixedTick(fixedDeltaTime);
                    }
                    catch (Exception e)
                    {
                        GLogger.LogException(e, LogTag.FRAMEWORK,
                            $"TickRegistry.FixedTick: {fixedTickable.GetType().Name} threw");
                    }
                }
            }
            finally
            {
                _fixedTickBuffer.Clear();
                _removedFixedTickables.Clear();
                _fixedTicking = false;
            }
        }

        /// <summary>
        /// 清空全部注册。若在派发过程中调用，本帧剩余的 Tick 一律不再执行，
        /// 与逐个 Unregister 的语义保持一致。
        /// </summary>
        public void Clear()
        {
            if (_ticking)
                _removedTickables.UnionWith(_tickables);
            if (_fixedTicking)
                _removedFixedTickables.UnionWith(_fixedTickables);

            _tickables.Clear();
            _fixedTickables.Clear();
        }
    }
}
