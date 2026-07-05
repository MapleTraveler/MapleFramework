using System;
using System.Collections.Generic;

namespace Maple.Core
{
    /// <summary>
    /// IConfigProvider 的默认实现：纯 C# 内存注册表，零依赖，与 EventBus / PoolProvider 同属核心基建。
    /// 内部以 typeof(TConfig) 为 key 存放各表；泛型 TKey 保证枚举 / int 键查询不装箱（避免 GC）。
    /// </summary>
    public sealed class ConfigProvider : IConfigProvider
    {
        private readonly Dictionary<Type, object> _tables = new();

        public void Register<TKey, TConfig>(IReadOnlyDictionary<TKey, TConfig> table)
        {
            if (table == null)
            {
                GLogger.LogWarning(LogTag.CONFIG,
                    $"ConfigProvider.Register: 传入的 {typeof(TConfig).Name} 表为 null，已忽略。");
                return;
            }

            var type = typeof(TConfig);
            if (_tables.ContainsKey(type))
                GLogger.LogWarning(LogTag.CONFIG,
                    $"ConfigProvider.Register: {type.Name} 表已存在，将被覆盖。");

            _tables[type] = table;
            GLogger.LogInfo(LogTag.CONFIG,
                $"ConfigProvider: 注册 {type.Name} 表，共 {table.Count} 条。");
        }

        public bool TryGet<TKey, TConfig>(TKey key, out TConfig config)
        {
            config = default;
            if (_tables.TryGetValue(typeof(TConfig), out var boxed)
                && boxed is IReadOnlyDictionary<TKey, TConfig> dict)
            {
                return dict.TryGetValue(key, out config);
            }
            return false;
        }

        public TConfig Get<TKey, TConfig>(TKey key)
        {
            if (TryGet<TKey, TConfig>(key, out var config))
                return config;

            GLogger.LogWarning(LogTag.CONFIG,
                $"ConfigProvider.Get: 未找到配置 {typeof(TConfig).Name}[{key}]，返回默认值。");
            return default;
        }

        public IReadOnlyDictionary<TKey, TConfig> GetTable<TKey, TConfig>()
        {
            if (_tables.TryGetValue(typeof(TConfig), out var boxed)
                && boxed is IReadOnlyDictionary<TKey, TConfig> dict)
            {
                return dict;
            }

            GLogger.LogWarning(LogTag.CONFIG,
                $"ConfigProvider.GetTable: {typeof(TConfig).Name} 表未注册，返回 null。");
            return null;
        }

        public bool Has<TConfig>() => _tables.ContainsKey(typeof(TConfig));

        public void Clear() => _tables.Clear();
    }
}
