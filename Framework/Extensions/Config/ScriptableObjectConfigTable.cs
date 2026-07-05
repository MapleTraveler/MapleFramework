using System.Collections.Generic;
using Maple.Core;
using UnityEngine;

namespace Maple.Extensions
{
    /// <summary>
    /// 可选便利件：把"一组条目 + 键选择器"变成可直接 Register 进 IConfigProvider 的字典。
    /// 用法：游戏层定义具体子类（否则 Unity 无法把开放泛型 SO 存成资源），实现 GetKey，
    /// 加 [CreateAssetMenu] 后即可在编辑器里配表；运行时经 IResourceLoader 加载并 BuildTable()。
    /// <para>
    /// 这只是抽掉"List → Dictionary"的样板，框架不绑定你的表结构；不用它、手动建字典也完全可以。
    /// </para>
    /// </summary>
    /// <typeparam name="TKey">键类型（int / string / enum 等）。</typeparam>
    /// <typeparam name="TEntry">条目类型（可序列化的类或结构体）。</typeparam>
    public abstract class ScriptableObjectConfigTable<TKey, TEntry> : ScriptableObject
    {
        [SerializeField] protected List<TEntry> entries = new();

        /// <summary> 子类实现：从一个条目中取出它的键。 </summary>
        protected abstract TKey GetKey(TEntry entry);

        /// <summary> 把条目列表构建成只读字典；重复键告警并保留首个。 </summary>
        public IReadOnlyDictionary<TKey, TEntry> BuildTable()
        {
            var dict = new Dictionary<TKey, TEntry>(entries?.Count ?? 0);
            if (entries == null) return dict;

            foreach (var entry in entries)
            {
                var key = GetKey(entry);
                if (dict.ContainsKey(key))
                {
                    GLogger.LogWarning(LogTag.CONFIG,
                        $"{GetType().Name}: 重复键 '{key}' 已忽略，保留首个条目。");
                    continue;
                }
                dict[key] = entry;
            }
            return dict;
        }
    }
}
