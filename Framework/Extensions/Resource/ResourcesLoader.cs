using System;
using System.Collections;
using System.Collections.Generic;
using Maple.Core;
using UnityEngine;

namespace Maple.Extensions
{
    /// <summary>
    /// IResourceLoader 的默认实现，基于 Unity Resources 文件夹。
    /// 适合原型开发和小项目，正式项目建议替换为 Addressables 或 AB 包实现。
    /// </summary>
    public class ResourcesLoader : IResourceLoader
    {
        private readonly Dictionary<string, UnityEngine.Object> _cache = new();

        public T Load<T>(string path) where T : class
        {
            if (_cache.TryGetValue(path, out var cached))
                return cached as T;

            var asset = Resources.Load(path);
            if (asset == null)
            {
                GLogger.LogWarning(LogTag.FRAMEWORK, $"ResourcesLoader: asset not found at '{path}'");
                return null;
            }

            _cache[path] = asset;
            return asset as T;
        }

        public void LoadAsync<T>(string path, Action<T> onComplete) where T : class
        {
            if (_cache.TryGetValue(path, out var cached))
            {
                onComplete?.Invoke(cached as T);
                return;
            }

            var request = Resources.LoadAsync(path);
            request.completed += _ =>
            {
                if (request.asset != null)
                    _cache[path] = request.asset;
                else
                    GLogger.LogWarning(LogTag.FRAMEWORK, $"ResourcesLoader: async load failed for '{path}'");

                onComplete?.Invoke(request.asset as T);
            };
        }

        public void Release(string path)
        {
            if (_cache.Remove(path, out var asset) && asset != null)
                Resources.UnloadAsset(asset);
        }

        public void ReleaseAll()
        {
            _cache.Clear();
            Resources.UnloadUnusedAssets();
        }
    }
}
