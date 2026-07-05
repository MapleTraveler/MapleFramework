using System;
using System.Threading.Tasks;

namespace Maple.Core
{
    /// <summary>
    /// 场景加载后端抽象。可替换实现：SceneManager（默认，场景来自 Build Settings）、
    /// 未来的 Addressables 等。上层（SceneFlowService）只依赖此接口，切换后端零改动。
    /// <para>
    /// 只提供异步加载：不同后端对"同步加载场景"的支持不一致（Addressables 无同步场景 API），
    /// 抽象须诚实反映后端最弱能力，故不承诺同步接口。见 ADR-006。
    /// </para>
    /// </summary>
    public interface ISceneLoader
    {
        /// <summary>
        /// 以单场景模式（Single）异步加载指定场景。
        /// </summary>
        /// <param name="sceneKey">场景标识（SceneManager 后端下为 Build Settings 中的场景名）。</param>
        /// <param name="progress">可选进度回调，取值 0..1。</param>
        Task LoadSceneAsync(string sceneKey, IProgress<float> progress = null);
    }
}
