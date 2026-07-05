using System;
using System.Text;
using Maple.Core;
using Newtonsoft.Json;

namespace Maple.Extensions
{
    /// <summary>
    /// 基于 Newtonsoft.Json 的 ISerializer 实现。
    /// 支持带 get/set 属性的 DTO，与 JsonUtility 相比更适合和 ASP.NET / 共享合约库配合。
    /// </summary>
    public class NewtonsoftJsonSerializer : ISerializer
    {
        private static readonly Encoding UTF8 = Encoding.UTF8;
        
        public byte[] Serialize<T>(T obj)
        {
            if (obj == null)
                return Array.Empty<byte>();
            var json = JsonConvert.SerializeObject(obj);
            return UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length == 0)
                return default;
            var json = UTF8.GetString(data);
            return string.IsNullOrEmpty(json) ? default : JsonConvert.DeserializeObject<T>(json);
        }
    }
}