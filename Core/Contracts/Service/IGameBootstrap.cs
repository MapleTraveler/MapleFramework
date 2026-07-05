namespace Maple.Core
{
    /// <summary>
    /// 游戏层启动入口接口。
    /// 由 GameRoot 在所有 IFrameworkService 初始化完成后统一扫描并调用 Bootstrap()，
    /// 用于注册游戏层服务（如 LLMAgentService），保证可安全依赖任何框架级服务。
    /// </summary>
    public interface IGameBootstrap
    {
        /// <summary> 启动优先级，数值越小越先执行。默认 0。 </summary>
        int BootOrder => 0;

        void Bootstrap();
    }
}
