using System;
using System.Collections;
using System.Collections.Generic;
using Maple.Core;
using UnityEngine;

namespace Maple.Extensions
{
    /// <summary>
    /// IResourceLoader 的 AssetBundle 实现。
    /// 支持依赖自动加载、引用计数卸载、异步加载去重。
    /// 需要挂到 MonoBehaviour 上以支持协程（通过 CoroutineHost 驱动）。
    /// </summary>
    public class AssetBundleLoader : IResourceLoader
    {
        private readonly string _basePath;
        private readonly string _manifestBundleName;
        private readonly MonoBehaviour _coroutineHost;

        private AssetBundle _manifestBundle;
        private AssetBundleManifest _manifest;

        private readonly Dictionary<string, AssetBundleRef> _loadedBundles = new();
        private readonly Dictionary<string, AssetBundleCreateRequest> _loadingBundles = new();

        /// <param name="basePath">AB 包存放目录（如 Application.streamingAssetsPath + "/AssetBundles/"）</param>
        /// <param name="manifestBundleName">主包名（如 "PC"、"Android"）</param>
        /// <param name="coroutineHost">用于启动协程的 MonoBehaviour</param>
        public AssetBundleLoader(string basePath, string manifestBundleName, MonoBehaviour coroutineHost)
        {
            _basePath = basePath;
            _manifestBundleName = manifestBundleName;
            _coroutineHost = coroutineHost;

            LoadManifest();
        }

        #region IResourceLoader

        public T Load<T>(string path) where T : class
        {
            ParsePath(path, out var bundleName, out var assetName);

            EnsureDependenciesLoaded(bundleName);
            var bundle = LoadBundleSync(bundleName);
            return bundle?.LoadAsset(assetName) as T;
        }

        public void LoadAsync<T>(string path, Action<T> onComplete) where T : class
        {
            ParsePath(path, out var bundleName, out var assetName);
            _coroutineHost.StartCoroutine(LoadAsyncCoroutine(bundleName, assetName, onComplete));
        }

        public void Release(string path)
        {
            ParsePath(path, out var bundleName, out _);
            UnloadBundle(bundleName);
        }

        public void ReleaseAll()
        {
            AssetBundle.UnloadAllAssetBundles(false);
            _loadedBundles.Clear();
            _loadingBundles.Clear();
            _manifestBundle = null;
            _manifest = null;
        }

        #endregion

        #region Internal

        private void LoadManifest()
        {
            _manifestBundle = AssetBundle.LoadFromFile(_basePath + _manifestBundleName);
            if (_manifestBundle == null)
            {
                GLogger.LogError(LogTag.FRAMEWORK, $"AssetBundleLoader: failed to load manifest bundle at '{_basePath + _manifestBundleName}'");
                return;
            }
            _manifest = _manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        }

        /// <summary>
        /// 路径格式：bundleName/assetName（如 "characters/hero_prefab"）
        /// </summary>
        private static void ParsePath(string path, out string bundleName, out string assetName)
        {
            int idx = path.IndexOf('/');
            if (idx < 0)
            {
                bundleName = path;
                assetName = path;
            }
            else
            {
                bundleName = path.Substring(0, idx);
                assetName = path.Substring(idx + 1);
            }
        }

        private void EnsureDependenciesLoaded(string bundleName)
        {
            if (_manifest == null) return;
            var deps = _manifest.GetAllDependencies(bundleName);
            foreach (var dep in deps)
                LoadBundleSync(dep);
        }

        private AssetBundle LoadBundleSync(string bundleName)
        {
            if (_loadedBundles.TryGetValue(bundleName, out var info))
            {
                info.Retain();
                return info.Bundle;
            }

            var bundle = AssetBundle.LoadFromFile(_basePath + bundleName);
            if (bundle == null)
            {
                GLogger.LogError(LogTag.FRAMEWORK, $"AssetBundleLoader: failed to load bundle '{bundleName}'");
                return null;
            }

            _loadedBundles[bundleName] = new AssetBundleRef(bundle);
            return bundle;
        }

        private IEnumerator LoadBundleAsyncDedup(string bundleName)
        {
            if (_loadedBundles.ContainsKey(bundleName))
            {
                _loadedBundles[bundleName].Retain();
                yield break;
            }

            if (_loadingBundles.TryGetValue(bundleName, out var existing))
            {
                yield return existing;
                if (_loadedBundles.TryGetValue(bundleName, out var loaded))
                    loaded.Retain();
                yield break;
            }

            var request = AssetBundle.LoadFromFileAsync(_basePath + bundleName);
            _loadingBundles[bundleName] = request;
            yield return request;
            _loadingBundles.Remove(bundleName);

            if (request.assetBundle != null)
                _loadedBundles[bundleName] = new AssetBundleRef(request.assetBundle);
            else
                GLogger.LogError(LogTag.FRAMEWORK, $"AssetBundleLoader: async load failed for '{bundleName}'");
        }

        private IEnumerator LoadAsyncCoroutine<T>(string bundleName, string assetName, Action<T> onComplete) where T : class
        {
            if (_manifest != null)
            {
                var deps = _manifest.GetAllDependencies(bundleName);
                foreach (var dep in deps)
                    yield return LoadBundleAsyncDedup(dep);
            }

            yield return LoadBundleAsyncDedup(bundleName);

            if (!_loadedBundles.TryGetValue(bundleName, out var bundleRef))
            {
                onComplete?.Invoke(null);
                yield break;
            }

            var assetRequest = bundleRef.Bundle.LoadAssetAsync(assetName);
            yield return assetRequest;
            onComplete?.Invoke(assetRequest.asset as T);
        }

        private void UnloadBundle(string bundleName)
        {
            if (!_loadedBundles.TryGetValue(bundleName, out var info)) return;

            info.Release();
            if (info.CanUnload)
            {
                info.Unload();
                _loadedBundles.Remove(bundleName);
            }

            if (_manifest == null) return;
            var deps = _manifest.GetAllDependencies(bundleName);
            foreach (var dep in deps)
            {
                if (_loadedBundles.TryGetValue(dep, out var depInfo))
                {
                    depInfo.Release();
                    if (depInfo.CanUnload)
                    {
                        depInfo.Unload();
                        _loadedBundles.Remove(dep);
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// AssetBundle 引用计数封装
        /// </summary>
        private class AssetBundleRef
        {
            public AssetBundle Bundle { get; private set; }
            private int _refCount;

            public AssetBundleRef(AssetBundle bundle)
            {
                Bundle = bundle;
                _refCount = 1;
            }

            public void Retain() => _refCount++;
            public void Release() => _refCount--;
            public bool CanUnload => _refCount <= 0;

            public void Unload(bool unloadAllLoadedObjects = false)
            {
                Bundle?.Unload(unloadAllLoadedObjects);
                Bundle = null;
            }
        }
    }
}
