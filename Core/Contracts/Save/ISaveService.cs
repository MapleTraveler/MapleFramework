namespace Maple.Core
{
    /// <summary>
    /// 存档服务契约。提供按槽位（slot）整存整取的持久化能力。
    /// 数据结构由游戏层定义，框架不绑定任何具体模型。
    /// 通过 ServiceHub.Get&lt;ISaveService&gt;() 获取实例。
    /// </summary>
    public interface ISaveService
    {
        /// <summary>
        /// 将 <paramref name="data"/> 写入指定槽位，覆盖原有内容。
        /// 实现需保证写入的原子性（写一半失败时不破坏已有存档）。
        /// </summary>
        /// <param name="slot">槽位名，对应一份独立存档（如 "progress"、"settings"）。</param>
        /// <param name="data">要保存的数据对象，需可被 ISerializer 序列化。</param>
        void Save<T>(string slot, T data);

        /// <summary>
        /// 读取指定槽位的数据。槽位不存在或读取失败时返回 <paramref name="fallback"/>。
        /// </summary>
        /// <param name="slot">槽位名。</param>
        /// <param name="fallback">槽位不存在时的回退值（通常传 new T() 表示初始状态）。</param>
        /// <returns>反序列化得到的数据，或回退值。</returns>
        T Load<T>(string slot, T fallback = default);

        /// <summary>判断指定槽位是否存在已保存的数据。</summary>
        bool Exists(string slot);

        /// <summary>删除指定槽位的存档。槽位不存在时静默忽略。</summary>
        void Delete(string slot);
    }
}
