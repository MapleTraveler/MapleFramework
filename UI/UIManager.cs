using System;
using System.Collections.Generic;
using Maple.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Maple.UI
{
    /// <summary>
    /// 全局 UI 管理器。挂在 GameRoot 同物体或子物体上，由 GameRoot 通过 IFrameworkService 统一初始化。
    /// 自动创建 Canvas 层级结构，通过 IResourceLoader 按约定路径加载 UI Prefab。
    /// <para>
    /// 约定路径：PanelController → "UI/Panels/{类名}"，WindowController → "UI/Windows/{类名}"
    /// </para>
    /// </summary>
    public sealed class UIManager : MonoBehaviour, IFrameworkService
    {
        private IResourceLoader _loader;
        private PanelLayer _panelLayer;
        private WindowLayer _windowLayer;

        private readonly Dictionary<Type, UIController> _instances = new();
        private readonly Dictionary<Type, string> _pathCache = new();

        private Canvas _canvas;
        private bool _initialized;
        private Scene _currentScene;

        void IFrameworkService.Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            _loader = ServiceHub.Require<IResourceLoader>();

            BuildHierarchy();

            _panelLayer = new PanelLayer(_canvas.transform.Find("Panels"));
            _windowLayer = new WindowLayer(_canvas.transform.Find("Windows"));

            ServiceHub.Register<UIManager>(this);
            _currentScene = SceneManager.GetActiveScene();
            SceneManager.activeSceneChanged += OnActiveSceneChanged;

            GLogger.LogInfo(LogTag.UI, "UIManager initialized.");
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        // ── 公开 API ──

        /// <summary> 显示指定类型的 UI，首次调用时自动加载 Prefab 并缓存实例 </summary>
        public T Show<T>(object properties = null) where T : UIController
        {
            var ctrl = GetOrCreate<T>();
            if (ctrl == null) return null;

            if (ctrl is PanelController panel)
                _panelLayer.Show(panel, properties);
            else if (ctrl is WindowController window)
                _windowLayer.Show(window, properties);

            return ctrl;
        }

        /// <summary> 隐藏指定类型的 UI </summary>
        public void Hide<T>() where T : UIController
        {
            if (!_instances.TryGetValue(typeof(T), out var ctrl)) return;

            if (ctrl is PanelController panel)
                _panelLayer.Hide(panel);
            else if (ctrl is WindowController window)
                _windowLayer.Hide(window);
        }

        /// <summary> 获取已加载的 UI 实例，未加载返回 null </summary>
        public T Get<T>() where T : UIController
        {
            return _instances.TryGetValue(typeof(T), out var ctrl) ? (T)ctrl : null;
        }

        /// <summary> 查询指定类型的 UI 是否正在显示 </summary>
        public bool IsShowing<T>() where T : UIController
        {
            return _instances.TryGetValue(typeof(T), out var ctrl) && ctrl.IsVisible;
        }

        /// <summary> 关闭所有窗口并清空窗口栈 </summary>
        public void CloseAllWindows() => _windowLayer.CloseAll();

        /// <summary> 隐藏所有面板 </summary>
        public void HideAllPanels() => _panelLayer.HideAll();

        // ── 内部逻辑 ──

        private T GetOrCreate<T>() where T : UIController
        {
            if (_instances.TryGetValue(typeof(T), out var cached))
                return (T)cached;

            var path = ResolvePath(typeof(T));
            var prefab = _loader.Load<GameObject>(path);
            if (prefab == null)
            {
                GLogger.LogError(LogTag.UI, $"UIManager: Prefab not found for {typeof(T).Name} at '{path}'");
                return null;
            }

            var parent = typeof(PanelController).IsAssignableFrom(typeof(T))
                ? _panelLayer.Root
                : _windowLayer.Root;

            var go = Instantiate(prefab, parent);
            go.name = typeof(T).Name;
            StretchFull(go.transform as RectTransform);

            var ctrl = go.GetComponent<T>();
            if (ctrl == null)
            {
                GLogger.LogError(LogTag.UI, $"UIManager: Prefab at '{path}' missing component {typeof(T).Name}");
                Destroy(go);
                return null;
            }

            ctrl.OnCloseRequest = () =>
            {
                if (ctrl is PanelController p) _panelLayer.Hide(p);
                else if (ctrl is WindowController w) _windowLayer.Hide(w);
            };

            go.SetActive(false);
            _instances[typeof(T)] = ctrl;
            GLogger.LogInfo(LogTag.UI, $"UIManager: Created {typeof(T).Name} from '{path}'");
            return ctrl;
        }

        private string ResolvePath(Type type)
        {
            if (_pathCache.TryGetValue(type, out var cached))
                return cached;

            var folder = typeof(PanelController).IsAssignableFrom(type) ? "Panels" : "Windows";
            var path = $"UI/{folder}/{type.Name}";
            _pathCache[type] = path;
            return path;
        }

        private void OnActiveSceneChanged(Scene from, Scene to)
        {
            // Unity 启动阶段可能触发 invalid -> 当前场景的 active scene 事件。
            // 这不是业务场景切换，不能清理刚创建的 Scene 生命周期 UI。
            if (!from.IsValid())
            {
                _currentScene = to;
                GLogger.LogInfo(LogTag.UI, $"UIManager: ignored initial scene activation -> {to.name}");
                return;
            }

            if (_currentScene.IsValid() && _currentScene == to)
                return;

            _currentScene = to;

            _panelLayer.ClearScoped();
            _windowLayer.ClearScoped();

            var toRemove = new List<Type>();
            foreach (var kvp in _instances)
            {
                if (kvp.Value.LifeScope == UILifeScope.Scene)
                {
                    if (kvp.Value.gameObject != null)
                        Destroy(kvp.Value.gameObject);
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var type in toRemove)
                _instances.Remove(type);
        }

        private void BuildHierarchy()
        {
            var canvasGO = new GameObject("UICanvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 0;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var panelGO = new GameObject("Panels");
            panelGO.transform.SetParent(_canvas.transform, false);
            var panelRect = panelGO.AddComponent<RectTransform>();
            StretchFull(panelRect);

            var windowGO = new GameObject("Windows");
            windowGO.transform.SetParent(_canvas.transform, false);
            var windowRect = windowGO.AddComponent<RectTransform>();
            StretchFull(windowRect);
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        
    }
}
