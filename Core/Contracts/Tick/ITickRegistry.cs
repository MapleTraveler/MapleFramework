namespace Maple.Core
{
    /// <summary>
    /// 提供 Tick 注册能力，由 GameRoot 实现并注册到 ServiceHub。
    /// 需要注册/反注册 ITickable、IFixedTickable 的模块通过 ServiceHub.Get&lt;ITickRegistry&gt;() 获取。
    ///
    /// 派发语义（实现必须保证，调用方可以依赖）：
    /// <list type="bullet">
    /// <item>注册：派发过程中注册的对象从下一轮派发起才被调用，不在当前这一轮被调用。</item>
    /// <item>注销：立即生效。派发过程中注销的对象，本轮尚未执行的 Tick 不再执行。</item>
    /// <item>顺序：按注册先后调用；同一实例重复注册只生效一次。</item>
    /// <item>同一轮内先注销再重新注册同一实例：本轮不再被调用，从下一轮起恢复。
    /// 这是上面两条规则的交叉结果，不是缺陷。</item>
    /// <item>派发过程中不可重入驱动派发。</item>
    /// </list>
    /// 因此在 Tick 内部注册或注销（含注销自己）是受支持的用法，不会漏掉或重复调用其它对象。
    /// </summary>
    public interface ITickRegistry
    {
        void Register(ITickable tickable);
        void Unregister(ITickable tickable);
        void Register(IFixedTickable fixedTickable);
        void Unregister(IFixedTickable fixedTickable);
    }
}
