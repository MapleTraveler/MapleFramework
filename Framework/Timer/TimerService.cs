using System;
using System.Collections.Generic;
using Maple.Core;
using UnityEngine;

namespace Maple.Framework
{
    /// <summary>
    /// 定时器服务实现。挂载在 GameRoot 同 GameObject（或子级）上，由框架自动初始化。
    /// </summary>
    public sealed class TimerService : MonoBehaviour, IFrameworkService, ITickable, ITimerService
    {
        // IFrameworkService — 晚于 UIManager（默认 0）初始化，顺序无强依赖
        public int InitOrder => 10;

        private readonly List<TimerEntry> _timers = new List<TimerEntry>(32);
        private readonly List<int> _pendingRemoveIndices = new List<int>(8);
        private int _nextHandle = 1;
        private bool _paused;

        // ──────────────────────────────────────────────────────────────────────
        // IFrameworkService
        // ──────────────────────────────────────────────────────────────────────

        void IFrameworkService.Initialize()
        {
            ServiceHub.Register<ITimerService>(this);
            ServiceHub.Require<ITickRegistry>().Register(this);
            GLogger.LogInfo(LogTag.FRAMEWORK, "TimerService initialized.");
        }

        // ──────────────────────────────────────────────────────────────────────
        // ITimerService
        // ──────────────────────────────────────────────────────────────────────

        public int Schedule(float delay, Action onComplete, bool ignoreTimeScale = false)
        {
            delay = Mathf.Max(0f, delay);
            var entry = new TimerEntry
            {
                Handle = _nextHandle++,
                Remaining = delay,
                Interval = delay,
                RepeatCount = 1,
                IgnoreTimeScale = ignoreTimeScale,
                Callback = onComplete
            };
            _timers.Add(entry);
            return entry.Handle;
        }

        public int ScheduleRepeating(float interval, Action onTick, int repeatCount = -1,
            bool ignoreTimeScale = false)
        {
            interval = Mathf.Max(0.001f, interval); // 最小 1ms，防止无限触发
            var entry = new TimerEntry
            {
                Handle = _nextHandle++,
                Remaining = interval,
                Interval = interval,
                RepeatCount = repeatCount,
                IgnoreTimeScale = ignoreTimeScale,
                Callback = onTick
            };
            _timers.Add(entry);
            return entry.Handle;
        }

        public void Cancel(int handle)
        {
            for (int i = 0; i < _timers.Count; i++)
            {
                if (_timers[i].Handle == handle)
                {
                    _timers[i].Cancelled = true;
                    return;
                }
            }
        }

        public void PauseAll() => _paused = true;
        public void ResumeAll() => _paused = false;

        // ──────────────────────────────────────────────────────────────────────
        // ITickable
        // ──────────────────────────────────────────────────────────────────────

        public void Tick(float deltaTime)
        {
            if (_timers.Count == 0) return;

            // 提前缓存 unscaledDeltaTime，并与 maximumDeltaTime 对齐：
            // Time.deltaTime 受 maximumDeltaTime 封顶，但 Time.unscaledDeltaTime 不受封顶。
            // 若不对齐，在初始化帧或卡顿帧里 ignoreTimeScale=true 的计时器会连续触发，
            // 而 ignoreTimeScale=false 的计时器正常走，两者产生漂移。
            float unscaledDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);

            _pendingRemoveIndices.Clear();

            for (int i = 0; i < _timers.Count; i++)
            {
                TimerEntry entry = _timers[i];

                if (entry.Cancelled)
                {
                    _pendingRemoveIndices.Add(i);
                    continue;
                }

                if (_paused)
                    continue;

                float dt = entry.IgnoreTimeScale ? unscaledDelta : deltaTime;
                entry.Remaining -= dt;

                if (entry.Remaining > 0f)
                    continue;

                // 到期：触发回调
                try
                {
                    entry.Callback?.Invoke();
                }
                catch (Exception e)
                {
                    GLogger.LogException(e, LogTag.FRAMEWORK,
                        $"TimerService: 计时器回调异常 (handle={entry.Handle})");
                }

                // 处理重复逻辑
                if (entry.RepeatCount > 0)
                    entry.RepeatCount--;

                if (entry.RepeatCount == 0)
                {
                    // 执行次数耗尽，标记移除
                    _pendingRemoveIndices.Add(i);
                }
                else
                {
                    // 保留，重置剩余时间（保留超出部分避免漂移）
                    entry.Remaining += entry.Interval;
                }
            }

            // 逆序移除，保证索引有效
            for (int i = _pendingRemoveIndices.Count - 1; i >= 0; i--)
                _timers.RemoveAt(_pendingRemoveIndices[i]);
        }

        // ──────────────────────────────────────────────────────────────────────
        // MonoBehaviour 生命周期
        // ──────────────────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            var registry = ServiceHub.Get<ITickRegistry>();
            registry?.Unregister(this);
        }

        // ──────────────────────────────────────────────────────────────────────
        // 内部数据
        // ──────────────────────────────────────────────────────────────────────

        private class TimerEntry
        {
            public int Handle;
            public float Remaining;
            public float Interval;
            /// <summary>剩余执行次数。-1 = 无限。</summary>
            public int RepeatCount;
            public bool IgnoreTimeScale;
            public Action Callback;
            public bool Cancelled;
        }
    }
}
