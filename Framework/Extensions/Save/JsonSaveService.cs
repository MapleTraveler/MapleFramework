using System;
using System.IO;
using Maple.Core;
using UnityEngine;

namespace Maple.Extensions
{
    /// <summary>
    /// 基于文件 + ISerializer 的存档实现。每个槽位对应一个 JSON 文件。
    /// 写入采用「临时文件 → 原子重命名」保证断电 / 崩溃时不破坏已有存档。
    /// 数据外层包一层 <see cref="SaveEnvelope{T}"/> 携带版本号，为未来迁移留口子。
    /// </summary>
    public sealed class JsonSaveService : ISaveService
    {
        /// <summary>当前存档格式版本。结构发生不兼容变更时递增。</summary>
        public const int CurrentVersion = 1;

        private const string FileExtension = ".json";
        private const string TempExtension = ".tmp";

        private readonly ISerializer _serializer;
        private readonly string _rootDir;

        /// <param name="serializer">对象 ↔ 字节序列化器，由外部注入（复用框架已注册的实现）。</param>
        /// <param name="subDirectory">存档子目录名，位于 Application.persistentDataPath 下。</param>
        public JsonSaveService(ISerializer serializer, string subDirectory = "Saves")
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _rootDir = Path.Combine(Application.persistentDataPath, subDirectory);
        }

        public void Save<T>(string slot, T data)
        {
            if (!ValidateSlot(slot)) return;

            try
            {
                Directory.CreateDirectory(_rootDir); // 已存在时无副作用

                var envelope = new SaveEnvelope<T>
                {
                    Version = CurrentVersion,
                    Payload = data
                };

                byte[] bytes = _serializer.Serialize(envelope);

                string targetPath = GetPath(slot);
                string tempPath = targetPath + TempExtension;

                // 1) 先写临时文件（WriteAllBytes 内部会 flush+close）
                File.WriteAllBytes(tempPath, bytes);

                // 2) 原子替换：操作系统保证要么旧文件完好、要么新文件完整，不会出现半截文件
                AtomicReplace(tempPath, targetPath);

                GLogger.LogInfo(LogTag.SAVE, $"已保存槽位 '{slot}'（{bytes.Length} 字节）");
            }
            catch (Exception e)
            {
                GLogger.LogException(e, LogTag.SAVE, $"保存槽位 '{slot}' 失败");
            }
        }

        public T Load<T>(string slot, T fallback = default)
        {
            if (!ValidateSlot(slot)) return fallback;

            string path = GetPath(slot);
            if (!File.Exists(path))
                return fallback;

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                var envelope = _serializer.Deserialize<SaveEnvelope<T>>(bytes);

                if (envelope == null)
                    return fallback;

                if (envelope.Version != CurrentVersion)
                {
                    // 这一期不做迁移，仅记录。未来可在此按版本号转换 Payload。
                    GLogger.LogWarning(LogTag.SAVE,
                        $"槽位 '{slot}' 版本为 {envelope.Version}，当前为 {CurrentVersion}（暂未迁移）");
                }

                return envelope.Payload;
            }
            catch (Exception e)
            {
                // 存档损坏不应让游戏崩溃，返回回退值并记录
                GLogger.LogException(e, LogTag.SAVE, $"读取槽位 '{slot}' 失败，返回回退值");
                return fallback;
            }
        }

        public bool Exists(string slot)
        {
            return ValidateSlot(slot) && File.Exists(GetPath(slot));
        }

        public void Delete(string slot)
        {
            if (!ValidateSlot(slot)) return;

            try
            {
                string path = GetPath(slot);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    GLogger.LogInfo(LogTag.SAVE, $"已删除槽位 '{slot}'");
                }
            }
            catch (Exception e)
            {
                GLogger.LogException(e, LogTag.SAVE, $"删除槽位 '{slot}' 失败");
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // 内部辅助
        // ──────────────────────────────────────────────────────────────────────

        private string GetPath(string slot) => Path.Combine(_rootDir, slot + FileExtension);

        private static bool ValidateSlot(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                GLogger.LogError(LogTag.SAVE, "槽位名为空，操作已忽略");
                return false;
            }

            if (slot.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                GLogger.LogError(LogTag.SAVE, $"槽位名 '{slot}' 含非法文件名字符，操作已忽略");
                return false;
            }

            return true;
        }

        private static void AtomicReplace(string tempPath, string targetPath)
        {
            if (File.Exists(targetPath))
            {
                // File.Replace 在同卷上是原子操作；不保留备份
                File.Replace(tempPath, targetPath, null);
            }
            else
            {
                // 目标不存在时 Replace 会抛异常，改用 Move（同样原子）
                File.Move(tempPath, targetPath);
            }
        }

        /// <summary>存档信封：包裹游戏层数据并携带版本号，游戏层无感。</summary>
        [Serializable]
        private class SaveEnvelope<T>
        {
            public int Version;
            public T Payload;
        }
    }
}
