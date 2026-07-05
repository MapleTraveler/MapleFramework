using System;
using System.Threading.Tasks;

namespace Maple.Core
{
    /// <summary>
    /// 场景流程服务：驱动单场景切换并对外广播加载进度 / 完成事件。
    /// <para>
    /// 刻意保持"UI 无关 + 游戏规则无关"：本服务不认识任何游戏状态枚举，也不直接操作 UI。
    /// "切哪个场景""是否显示 Loading 界面"等编排由游戏层完成（订阅事件 / 调用 LoadAsync）。
    /// 见 ADR-006。
    /// </para>
    /// </summary>
    public interface ISceneFlowService
    {
        /// <summary> 是否正在加载场景（用于防重入 / UI 状态判断）。 </summary>
        bool IsLoading { get; }

        /// <summary> 以单场景模式异步切换到目标场景。加载中重复调用将被忽略。 </summary>
        Task LoadAsync(string sceneKey);

        /// <summary> 开始加载时触发，参数为目标场景 key。 </summary>
        event Action<string> OnLoadStarted;

        /// <summary> 加载进度更新，取值 0..1。 </summary>
        event Action<float> OnLoadProgress;

        /// <summary> 加载并激活完成时触发，参数为已加载场景 key。 </summary>
        event Action<string> OnLoadCompleted;
    }
}
