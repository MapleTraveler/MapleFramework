using System;
using System.Collections;
using System.Collections.Generic;
using Maple.Core;
using UnityEngine;

namespace Maple.Extensions
{
    /// <summary>
    /// 创建 <see cref="AudioService"/> 时的项目配置。
    /// 双 BGM 音源由框架内部持有，项目只提供加载策略和默认音量。
    /// </summary>
    public struct AudioServiceSettings
    {
        public Func<string, AudioClip> ClipLoader;
        public float BgmVolume;
        public float SfxVolume;
    }

    /// <summary>
    /// IAudioService 的默认实现。
    /// 双 AudioSource 交叉淡入淡出 BGM，音频缓存，音量控制。
    /// 挂到 GameObject 上，Awake 时自动注册到 ServiceHub。
    /// 音源由本组件自行创建或补齐；也可通过 Inspector 预接线。
    /// 资源默认从 Resources/Audio/ 加载，可通过 ClipLoader 委托替换加载方式。
    /// </summary>
    public class AudioService : MonoBehaviour, IAudioService
    {
        [Header("BGM")]
        [SerializeField] private AudioSource bgmSourceA;
        [SerializeField] private AudioSource bgmSourceB;

        [Header("SFX")]
        [SerializeField] private AudioSource sfxSource;

        [Header("Default Settings")]
        [SerializeField, Range(0f, 1f)] private float defaultBGMVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float defaultSFXVolume = 1f;

        private AudioSource _currentBgm;
        private AudioSource _nextBgm;
        private Coroutine _fadeCoroutine;

        private readonly Dictionary<string, AudioClip> _clipCache = new();

        /// <summary>
        /// 可替换的 AudioClip 加载委托。默认走 Resources.Load，
        /// 项目可替换为 Addressables 或 AssetBundle 加载。
        /// </summary>
        public Func<string, AudioClip> ClipLoader { get; set; }

        /// <summary>
        /// 创建并配置全局 2D 音频服务。音源由框架内部持有，就绪后注册到 ServiceHub。
        /// </summary>
        public static AudioService Create(Transform parent, AudioServiceSettings settings)
        {
            var go = new GameObject(nameof(AudioService));
            if (parent != null)
                go.transform.SetParent(parent, false);
            go.SetActive(false);

            var service = go.AddComponent<AudioService>();
            service.ClipLoader = settings.ClipLoader;
            service.defaultBGMVolume = Mathf.Clamp01(settings.BgmVolume);
            service.defaultSFXVolume = Mathf.Clamp01(settings.SfxVolume);

            go.SetActive(true);
            return service;
        }

        #region IAudioService

        public float BGMVolume
        {
            get => defaultBGMVolume;
            set
            {
                defaultBGMVolume = Mathf.Clamp01(value);
                if (_currentBgm != null && _currentBgm.isPlaying)
                    _currentBgm.volume = defaultBGMVolume;
            }
        }

        public float SFXVolume
        {
            get => defaultSFXVolume;
            set
            {
                defaultSFXVolume = Mathf.Clamp01(value);
                if (sfxSource != null)
                    sfxSource.volume = defaultSFXVolume;
            }
        }

        public void PlayBGM(string key, float fadeDuration = 1f)
        {
            var clip = LoadClip(key);
            if (clip == null) return;

            if (_currentBgm.clip == clip && _currentBgm.isPlaying)
                return;

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _nextBgm.clip = clip;
            _fadeCoroutine = StartCoroutine(CrossFade(fadeDuration));
        }

        public void StopBGM(float fadeDuration = 1f)
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            if (fadeDuration <= 0f)
            {
                _currentBgm.Stop();
                _currentBgm.clip = null;
                return;
            }

            _fadeCoroutine = StartCoroutine(FadeOut(fadeDuration));
        }

        public void PauseBGM()
        {
            _currentBgm.Pause();
        }

        public void ResumeBGM()
        {
            _currentBgm.UnPause();
        }

        public void PlaySFX(string key)
        {
            var clip = LoadClip(key);
            if (clip == null) return;

            sfxSource.PlayOneShot(clip, defaultSFXVolume);
        }

        #endregion

        #region Lifecycle

        private void Awake()
        {
            EnsureOwnedSources();
            InitSource(bgmSourceA);
            InitSource(bgmSourceB);

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;

            _currentBgm = bgmSourceA;
            _nextBgm = bgmSourceB;

            ServiceHub.Register<IAudioService>(this);
        }

        private void EnsureOwnedSources()
        {
            if (bgmSourceA == null)
                bgmSourceA = CreateOwnedSource("BgmA");
            if (bgmSourceB == null)
                bgmSourceB = CreateOwnedSource("BgmB");
            if (sfxSource == null)
                sfxSource = CreateOwnedSource("Sfx");
        }

        private AudioSource CreateOwnedSource(string childName)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private static void InitSource(AudioSource source)
        {
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
        }

        #endregion

        #region Internal

        private AudioClip LoadClip(string key)
        {
            if (_clipCache.TryGetValue(key, out var cached))
                return cached;

            AudioClip clip;
            if (ClipLoader != null)
            {
                clip = ClipLoader(key);
            }
            else
            {
                clip = Resources.Load<AudioClip>($"Audio/{key}");
            }

            if (clip == null)
            {
                GLogger.LogWarning(LogTag.FRAMEWORK, $"AudioService: clip not found for key '{key}'");
                return null;
            }

            _clipCache[key] = clip;
            return clip;
        }

        private IEnumerator CrossFade(float duration)
        {
            _nextBgm.volume = 0f;
            _nextBgm.Play();

            float time = 0f;
            float startVolume = _currentBgm.volume;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);

                _currentBgm.volume = Mathf.Lerp(startVolume, 0f, t);
                _nextBgm.volume = Mathf.Lerp(0f, defaultBGMVolume, t);

                yield return null;
            }

            _currentBgm.Stop();
            _currentBgm.volume = 0f;
            _nextBgm.volume = defaultBGMVolume;

            (_currentBgm, _nextBgm) = (_nextBgm, _currentBgm);
            _fadeCoroutine = null;
        }

        private IEnumerator FadeOut(float duration)
        {
            float time = 0f;
            float startVolume = _currentBgm.volume;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                _currentBgm.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            _currentBgm.Stop();
            _currentBgm.clip = null;
            _currentBgm.volume = 0f;
            _fadeCoroutine = null;
        }

        #endregion
    }
}
