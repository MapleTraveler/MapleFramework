namespace Maple.Core
{
    /// <summary>
    /// 非泛型基接口，用于统一管理所有池（如 PoolProvider 内部按 Type 存储）
    /// </summary>
    public interface IObjectPool
    {
        int CountInactive { get; }
        void Clear();
    }

    /// <summary>
    /// 泛型对象池接口，同时服务于通用池和实体工厂池
    /// </summary>
    public interface IObjectPool<T> : IObjectPool
    {
        T Get();
        void Return(T obj);
    }
}
