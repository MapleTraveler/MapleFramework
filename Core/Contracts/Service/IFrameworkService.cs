namespace Maple.Core
{
    /// <summary>
    /// 框架级 MonoBehaviour 服务接口。
    /// 实现此接口的组件不应在 Awake/Start 中自行初始化，
    /// 而是由 GameRoot 在完成 ServiceHub 注册后统一调用 Initialize()，确保时序可控。
    /// </summary>
    public interface IFrameworkService
    {
        /// <summary> 初始化优先级，数值越小越先执行。默认 0。 </summary>
        int InitOrder => 0;

        void Initialize();
    }
}
