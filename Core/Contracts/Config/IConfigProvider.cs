using System.Collections.Generic;

namespace Maple.Core
{
    /// <summary>
    /// 配置提供者：静态只读配置数据的"注册 + 按键索引"通用机制。
    /// 一种配置类型（TConfig）对应一张表，键类型（TKey）由游戏层决定（int / string / enum 皆可）。
    /// <para>
    /// 只负责索引，不负责"从哪加载"：如何把 SO / JSON / CSV 变成字典由游戏层完成后 Register 进来，
    /// 框架不绑定数据模型（见 ADR-007）。存放的应是不可变的配置模板，而非运行时可变状态。
    /// </para>
    /// </summary>
    public interface IConfigProvider
    {
        /// <summary> 注册一张配置表（以 TConfig 类型为 key，重复注册会覆盖并告警）。 </summary>
        void Register<TKey, TConfig>(IReadOnlyDictionary<TKey, TConfig> table);

        /// <summary> 按键取配置，未命中时告警并返回 default（不抛异常，避免中断游戏）。 </summary>
        TConfig Get<TKey, TConfig>(TKey key);

        /// <summary> 按键取配置，命中返回 true；调用方需严格处理缺失时用此重载。 </summary>
        bool TryGet<TKey, TConfig>(TKey key, out TConfig config);

        /// <summary> 取整张表，未注册时告警并返回 null。 </summary>
        IReadOnlyDictionary<TKey, TConfig> GetTable<TKey, TConfig>();

        /// <summary> 是否已注册指定类型的表。 </summary>
        bool Has<TConfig>();

        /// <summary> 清空所有已注册的表（用于测试 / 换表重置）。 </summary>
        void Clear();
    }
}
