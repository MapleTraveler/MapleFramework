using System;

namespace Maple.Core
{
    /// <summary>
    /// 定时器服务契约。提供延迟回调、循环调度与统一暂停能力。
    /// 通过 ServiceHub.Get&lt;ITimerService&gt;() 获取实例。
    /// </summary>
    public interface ITimerService
    {
        /// <summary>
        /// 延迟 <paramref name="delay"/> 秒后执行一次 <paramref name="onComplete"/>。
        /// </summary>
        /// <param name="delay">延迟秒数。</param>
        /// <param name="onComplete">到期时的回调。</param>
        /// <param name="ignoreTimeScale">
        /// true：使用 unscaledDeltaTime，不受 Time.timeScale 影响（适合 UI 动画、暂停界面倒计时）。
        /// false：使用 deltaTime，随游戏暂停而停止（适合技能冷却、Buff 持续时间）。
        /// </param>
        /// <returns>计时器句柄，可传给 Cancel 取消。</returns>
        int Schedule(float delay, Action onComplete, bool ignoreTimeScale = false);

        /// <summary>
        /// 每隔 <paramref name="interval"/> 秒执行一次 <paramref name="onTick"/>。
        /// </summary>
        /// <param name="interval">循环间隔秒数。</param>
        /// <param name="onTick">每次触发的回调。</param>
        /// <param name="repeatCount">执行次数，-1 表示无限循环。</param>
        /// <param name="ignoreTimeScale">同 Schedule 的 ignoreTimeScale 参数。</param>
        /// <returns>计时器句柄，可传给 Cancel 取消。</returns>
        int ScheduleRepeating(float interval, Action onTick, int repeatCount = -1,
            bool ignoreTimeScale = false);

        /// <summary>取消指定句柄对应的计时器。句柄无效时静默忽略。</summary>
        void Cancel(int handle);

        /// <summary>暂停所有计时器（包括 ignoreTimeScale 的）。</summary>
        void PauseAll();

        /// <summary>恢复所有计时器。</summary>
        void ResumeAll();
    }
}
