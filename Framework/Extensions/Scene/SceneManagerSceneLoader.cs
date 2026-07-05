using System;
using System.Threading.Tasks;
using Maple.Core;
using UnityEngine.SceneManagement;

namespace Maple.Extensions
{
    /// <summary>
    /// ISceneLoader 的默认实现，基于 UnityEngine 的 SceneManager，场景来自 Build Settings。
    /// 与 ResourcesLoader / 未来的 AddressablesLoader 并列，属于可替换的资源后端。
    /// </summary>
    public sealed class SceneManagerSceneLoader : ISceneLoader
    {
        public async Task LoadSceneAsync(string sceneKey, IProgress<float> progress = null)
        {
            if (string.IsNullOrEmpty(sceneKey))
                throw new ArgumentException("sceneKey 不能为空", nameof(sceneKey));

            var op = SceneManager.LoadSceneAsync(sceneKey, LoadSceneMode.Single);
            if (op == null)
                throw new InvalidOperationException(
                    $"场景 '{sceneKey}' 加载失败：请确认该场景已加入 Build Settings。");

            // AsyncOperation.progress 在加载阶段为 0..0.9，激活后 isDone 置真。
            while (!op.isDone)
            {
                progress?.Report(op.progress);
                await Task.Yield();
            }

            progress?.Report(1f);
        }
    }
}
