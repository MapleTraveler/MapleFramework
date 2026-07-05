namespace Maple.Core
{
    /// <summary>
    /// 通用序列化契约。将对象与字节互转，供网络、存档、配置等复用。
    /// 实现方需约定 T 的约束（如可序列化、无循环引用等）。
    /// </summary>
    public interface ISerializer
    {
        byte[] Serialize<T>(T obj);

        T Deserialize<T>(byte[] data);
    }
}