using System.Collections.Generic;
using System.Linq;
using Maple.Core;
using Maple.Extensions;
using UnityEngine;

namespace Maple.Framework
{
    /// <summary>
    /// 游戏唯一入口 MonoBehaviour：初始化 ServiceHub、注册核心服务、
    /// 扫描并初始化所有 IFrameworkService、驱动 ITickable/IFixedTickable。
    /// 场景中挂一个即可，不暴露静态 Instance，通过 ServiceHub.Get&lt;GameRoot&gt;() 获取。
    /// </summary>
    public class GameRoot : MonoBehaviour, ITickRegistry
    {
        private readonly List<ITickable> _tickables = new();
        private readonly List<IFixedTickable> _fixedTickables = new();

        [Tooltip("为 true 时跨场景不销毁，适合单例入口；为 false 时随场景卸载销毁并 Shutdown。")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Tooltip("启动时注册的 IResourceLoader 实现类型；选 None 则需在别处自行注册。")]
        [SerializeField] private EResourceLoaderType resourceLoaderType = EResourceLoaderType.Resources;

        [Header("Network Stack (Optional)")]
        [Tooltip("注册默认 JSON 序列化器（Newtonsoft）。不需要网络功能的项目可关闭。")]
        [SerializeField] private bool registerDefaultSerializer = true;

        [Tooltip("注册默认 HTTP 客户端（HttpRestClient）。依赖序列化器，关闭序列化器时此项自动无效。")]
        [SerializeField] private bool registerHttpClient = true;

        [Tooltip("HTTP 请求超时时间（秒），仅 Register Http Client = true 时生效。")]
        [SerializeField] private int httpTimeoutSeconds = 30;

        [Header("Persistence (Optional)")]
        [Tooltip("注册默认存档服务（JsonSaveService）。依赖序列化器，关闭序列化器时此项自动无效。")]
        [SerializeField] private bool registerSaveService = true;

        [Tooltip("存档子目录名，位于 Application.persistentDataPath 下。")]
        [SerializeField] private string saveSubDirectory = "Saves";

        [Header("Scene Flow (Optional)")]
        [Tooltip("注册场景流程服务（SceneFlowService）。依赖 ISceneLoader；Resource Loader Type = None 时无 ISceneLoader，此项自动无效。")]
        [SerializeField] private bool registerSceneFlow = true;

        private void Awake()
        {
            // ── 1. 核心服务初始化 ──
            if (!ServiceHub.IsReady)
                ServiceHub.Initialize();

            if (ServiceHub.Get<IResourceLoader>() == null)
            {
                switch (resourceLoaderType)
                {
                    case EResourceLoaderType.None:
                        break;
                    case EResourceLoaderType.Resources:
                        ServiceHub.Register<IResourceLoader>(new ResourcesLoader());
                        GLogger.LogInfo(LogTag.FRAMEWORK, "GameRoot: IResourceLoader (ResourcesLoader) registered.");
                        break;
                    default:
                        GLogger.LogWarning(LogTag.FRAMEWORK, $"GameRoot: resourceLoaderType={resourceLoaderType} not implemented, IResourceLoader not registered.");
                        break;
                }
            }
            
            // 注册 GameRoot 和 ITickRegistry（供框架服务注册自己为 Tickable/FixedTickable）。
            ServiceHub.Register<GameRoot>(this);
            ServiceHub.Register<ITickRegistry>(this);
            
            if (registerDefaultSerializer && ServiceHub.Get<ISerializer>() == null)
            {
                ServiceHub.Register<ISerializer>(new NewtonsoftJsonSerializer());
                GLogger.LogInfo(LogTag.FRAMEWORK, "GameRoot: ISerializer (NewtonsoftJsonSerializer) registered.");
            }

            if (registerHttpClient && ServiceHub.Get<IRequestResponseClient>() == null)
            {
                ServiceHub.Register<IRequestResponseClient>(
                    new HttpRestClient(ServiceHub.Require<ISerializer>(), httpTimeoutSeconds));
                GLogger.LogInfo(LogTag.FRAMEWORK, "GameRoot: IRequestResponseClient (HttpRestClient) registered.");
            }

            if (registerSaveService && ServiceHub.Get<ISaveService>() == null
                                    && ServiceHub.Get<ISerializer>() != null)
            {
                ServiceHub.Register<ISaveService>(
                    new JsonSaveService(ServiceHub.Require<ISerializer>(), saveSubDirectory));
                GLogger.LogInfo(LogTag.FRAMEWORK, "GameRoot: ISaveService (JsonSaveService) registered.");
            }

            // 场景加载后端：与 IResourceLoader 同源（同一 EResourceLoaderType），
            // 为 Phase 5 换 Addressables 时"一个枚举切两个后端"留位。None 分支不注册。
            if (ServiceHub.Get<ISceneLoader>() == null)
            {
                switch (resourceLoaderType)
                {
                    case EResourceLoaderType.None:
                        break;
                    case EResourceLoaderType.Resources:
                        ServiceHub.Register<ISceneLoader>(new SceneManagerSceneLoader());
                        GLogger.LogInfo(LogTag.FRAMEWORK, "GameRoot: ISceneLoader (SceneManagerSceneLoader) registered.");
                        break;
                    default:
                        GLogger.LogWarning(LogTag.FRAMEWORK, $"GameRoot: resourceLoaderType={resourceLoaderType} 无对应 ISceneLoader，未注册。");
                        break;
                }
            }

            if (registerSceneFlow && ServiceHub.Get<ISceneFlowService>() == null
                                  && ServiceHub.Get<ISceneLoader>() != null)
            {
                ServiceHub.Register<ISceneFlowService>(
                    new SceneFlowService(ServiceHub.Require<ISceneLoader>()));
                GLogger.LogInfo(LogTag.FRAMEWORK, "GameRoot: ISceneFlowService (SceneFlowService) registered.");
            }

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            // ── 2. 扫描并初始化所有 IFrameworkService（按 InitOrder 排序） ──
            InitializeFrameworkServices();

            // ── 3. 扫描并执行所有 IGameBootstrap（按 BootOrder 排序） ──
            InitializeGameBootstraps();

            GLogger.LogInfo(LogTag.FRAMEWORK, "GameRoot initialized.");
        }

        private void InitializeFrameworkServices()
        {
            var services = GetComponentsInChildren<MonoBehaviour>(true)
                .OfType<IFrameworkService>()
                .OrderBy(s => s.InitOrder);

            foreach (var svc in services)
            {
                svc.Initialize();
                GLogger.LogInfo(LogTag.FRAMEWORK, $"FrameworkService initialized: {svc.GetType().Name}");
            }
        }

        private void InitializeGameBootstraps()
        {
            var bootstraps = GetComponentsInChildren<MonoBehaviour>(true)
                .OfType<IGameBootstrap>()
                .OrderBy(b => b.BootOrder);

            foreach (var bootstrap in bootstraps)
            {
                bootstrap.Bootstrap();
                GLogger.LogInfo(LogTag.FRAMEWORK, $"GameBootstrap executed: {bootstrap.GetType().Name}");
            }
        }

        private void Update()
        {
            for (int i = 0; i < _tickables.Count; i++)
                _tickables[i].Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < _fixedTickables.Count; i++)
                _fixedTickables[i].FixedTick(Time.fixedDeltaTime);
        }

        public void Register(ITickable tickable)
        {
            if (tickable != null && !_tickables.Contains(tickable))
                _tickables.Add(tickable);
        }

        public void Unregister(ITickable tickable)
        {
            _tickables.Remove(tickable);
        }

        public void Register(IFixedTickable fixedTickable)
        {
            if (fixedTickable != null && !_fixedTickables.Contains(fixedTickable))
                _fixedTickables.Add(fixedTickable);
        }

        public void Unregister(IFixedTickable fixedTickable)
        {
            _fixedTickables.Remove(fixedTickable);
        }

        private void OnDestroy()
        {
            _tickables.Clear();
            _fixedTickables.Clear();
            ServiceHub.Shutdown();
        }
    }
}
