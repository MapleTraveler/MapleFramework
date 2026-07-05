using System;
using System.Text;
using Maple.Core;
using UnityEngine;

namespace Maple.Extensions
{
    /// <summary>
    /// 基于 Unity JsonUtility 的 ISerializer 实现。适用于简单 DTO，不支持字典、多态等复杂结构，只能序列化 public 字段（而不是属性）。
    /// </summary>
    public class JsonSerializer : ISerializer
    {
        private static readonly Encoding UTF8 = Encoding.UTF8;

        public byte[] Serialize<T>(T obj)
        {
            if (obj == null)
                return Array.Empty<byte>();

            var json = JsonUtility.ToJson(obj);
            return json != null ? UTF8.GetBytes(json) : Array.Empty<byte>();
        }

        public T Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length == 0)
                return default;

            var json = UTF8.GetString(data);
            return string.IsNullOrEmpty(json) ? default : JsonUtility.FromJson<T>(json);
        }
    }
}