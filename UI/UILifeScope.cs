namespace Maple.UI
{
    public enum UILifeScope
    {
        /// <summary> 场景切换时自动销毁（默认） </summary>
        Scene,

        /// <summary> 跟随 UIManager 生命周期，不随场景切换销毁 </summary>
        Global,
    }
}
