namespace Maple.Framework
{
    /// <summary>
    /// GameRoot 启动时注册的 IResourceLoader 实现类型。均为纯 C# 类，非 MonoBehaviour。
    /// </summary>
    public enum EResourceLoaderType
    {
        /// <summary> 不自动注册，由游戏层自行注册 IResourceLoader </summary>
        None,

        /// <summary> 注册 ResourcesLoader（基于 Resources 目录） </summary>
        Resources,

        /// <summary> 预留：AssetBundle，需在代码中自行注册 AssetBundleLoader 后选用 None </summary>
        // AssetBundle,

        /// <summary> 预留：Addressables </summary>
        // Addressables,
    }
}
