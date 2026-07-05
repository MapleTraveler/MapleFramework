namespace Maple.UI
{
    /// <summary>
    /// 窗口控制器基类。
    /// 窗口由 WindowLayer 以栈结构管理：新窗口打开时旧窗口压栈隐藏，关闭后自动恢复上一个。
    /// </summary>
    public abstract class WindowController : UIController
    {
        /// <summary> 是否为弹窗类型（预留，将来可扩展蒙黑层逻辑） </summary>
        public virtual bool IsPopup => false;
    }
}
