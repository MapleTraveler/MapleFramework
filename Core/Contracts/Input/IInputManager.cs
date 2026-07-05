namespace Maple.Core
{
    /// <summary>
    /// 输入管理器契约。
    /// TActionMap 为项目自定义的 ActionMap 枚举（如 Player, UI, Cutscene 等）。
    /// </summary>
    public interface IInputManager<in TActionMap> where TActionMap : struct
    {
        void Initialize();
        void SwitchActionMap(TActionMap mapType);
        void EnableInput();
        void DisableInput();
    }
}
