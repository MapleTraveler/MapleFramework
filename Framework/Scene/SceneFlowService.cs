using System;
using System.Threading.Tasks;
using Maple.Core;

namespace Maple.Framework
{
    /// <summary>
    /// ISceneFlowService 默认实现。纯 C# 类，通过构造注入 ISceneLoader（ADR-001 纪律，便于单测）。
    /// 只负责"切场景 + 广播进度/完成事件"，不认识游戏状态、不操作 UI（见 ADR-006）。
    /// 由 GameRoot 在组合根处构造并注册到 ServiceHub。
    /// </summary>
    public sealed class SceneFlowService : ISceneFlowService
    {
        private readonly ISceneLoader _loader;

        public bool IsLoading { get; private set; }

        public event Action<string> OnLoadStarted;
        public event Action<float> OnLoadProgress;
        public event Action<string> OnLoadCompleted;

        public SceneFlowService(ISceneLoader loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        public async Task LoadAsync(string sceneKey)
        {
            if (string.IsNullOrEmpty(sceneKey))
            {
                GLogger.LogWarning(LogTag.SCENE, "SceneFlowService.LoadAsync: sceneKey 为空，已忽略。");
                return;
            }

            if (IsLoading)
            {
                GLogger.LogWarning(LogTag.SCENE,
                    $"SceneFlowService.LoadAsync: 正在加载中，忽略对 '{sceneKey}' 的重复请求。");
                return;
            }

            IsLoading = true;
            OnLoadStarted?.Invoke(sceneKey);

            try
            {
                var progress = new Progress<float>(p => OnLoadProgress?.Invoke(p));
                await _loader.LoadSceneAsync(sceneKey, progress);
                OnLoadCompleted?.Invoke(sceneKey);
                GLogger.LogInfo(LogTag.SCENE, $"SceneFlowService: 场景 '{sceneKey}' 加载完成。");
            }
            catch (Exception e)
            {
                GLogger.LogException(e, LogTag.SCENE, $"SceneFlowService: 场景 '{sceneKey}' 加载失败。");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
