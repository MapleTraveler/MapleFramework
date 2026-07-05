using Maple.Core;
using UnityEngine;

namespace Maple.Framework
{
    /// <summary>
    /// 输入管理器抽象基类。
    /// TActionMap 由游戏定义（如 enum EActionMap { Player, UI, Cutscene }）。
    /// 
    /// 职责：
    ///   - 维护当前 ActionMap 状态
    ///   - 提供 Initialize / SwitchActionMap / Enable / Disable 的模板
    ///   - 通过 ServiceHub 注册，不暴露静态 Instance
    /// 
    /// 游戏层继承后：
    ///   - 实现 OnActionMapSwitch 完成具体的 InputSystem ActionMap 切换
    ///   - 如需响应游戏状态变化自动切 Map，在子类订阅 GameStateChangedEvent 自行处理
    /// </summary>
    public abstract class InputManagerBase<TActionMap> : MonoBehaviour, IInputManager<TActionMap>
        where TActionMap : struct
    {
        protected TActionMap CurrentMap { get; private set; }
        protected bool Initialized { get; private set; }

        public virtual void Initialize()
        {
            if (Initialized) return;
            Initialized = true;

            OnInitialize();
            GLogger.LogInfo(LogTag.FRAMEWORK, $"InputManager<{typeof(TActionMap).Name}> initialized.");
        }

        public void SwitchActionMap(TActionMap mapType)
        {
            if (Initialized && CurrentMap.Equals(mapType)) return;

            CurrentMap = mapType;
            OnActionMapSwitch(mapType);
        }

        public abstract void EnableInput();
        public abstract void DisableInput();

        /// <summary>
        /// 子类在此完成 InputSystem 的初始化（创建 InputActions、注册回调等）。
        /// </summary>
        protected abstract void OnInitialize();

        /// <summary>
        /// 子类在此完成具体 ActionMap 的 Enable/Disable 切换。
        /// </summary>
        protected abstract void OnActionMapSwitch(TActionMap mapType);

        protected virtual void OnDestroy()
        {
            Initialized = false;
        }
    }
}
