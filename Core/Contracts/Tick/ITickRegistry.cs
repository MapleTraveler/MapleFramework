namespace Maple.Core
{
    /// <summary>
    /// 提供 Tick 注册能力，由 GameRoot 实现并注册到 ServiceHub。
    /// 需要注册/反注册 ITickable、IFixedTickable 的模块通过 ServiceHub.Get&lt;ITickRegistry&gt;() 获取。
    /// </summary>
    public interface ITickRegistry
    {
        void Register(ITickable tickable);
        void Unregister(ITickable tickable);
        void Register(IFixedTickable fixedTickable);
        void Unregister(IFixedTickable fixedTickable);
    }
}
