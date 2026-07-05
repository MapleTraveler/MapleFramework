using System;
using UnityEngine;

namespace Maple.UI
{
    /// <summary>
    /// UI 控制器抽象基类。所有 Panel / Window 均继承此类。
    /// 框架通过 internal 方法驱动生命周期，游戏层通过重写 OnShow / OnHide / OnSetProperties 实现具体逻辑。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIController : MonoBehaviour
    {
        /// <summary> 子类可重写以声明生命周期范围，默认 Scene </summary>
        public virtual UILifeScope LifeScope => UILifeScope.Scene;

        public CanvasGroup CanvasGroup { get; private set; }
        public bool IsVisible { get; private set; }

        internal Action OnCloseRequest;

        protected virtual void Awake()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
        }

        // ── 框架内部驱动 ──

        internal void DoShow(object properties, Action onComplete)
        {
            gameObject.SetActive(true);
            IsVisible = true;
            if (properties != null) OnSetProperties(properties);
            OnShow(onComplete ?? NoOp);
        }

        internal void DoHide(Action onComplete)
        {
            IsVisible = false;
            OnHide(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }

        // ── 子类重写 ──

        /// <summary> 接收外部传入的数据，子类按需转型使用 </summary>
        protected virtual void OnSetProperties(object properties) { }

        /// <summary>
        /// 显示时调用。可在此执行入场动画，完成后必须调用 onComplete。
        /// 默认实现立即完成（无动画）。
        /// </summary>
        protected virtual void OnShow(Action onComplete) => onComplete();

        /// <summary>
        /// 隐藏时调用。可在此执行退场动画，完成后必须调用 onComplete。
        /// 默认实现立即完成（无动画）。
        /// </summary>
        protected virtual void OnHide(Action onComplete) => onComplete();

        /// <summary> 子类调用此方法请求关闭自身（Panel 直接隐藏 / Window 触发出栈） </summary>
        protected void Close() => OnCloseRequest?.Invoke();

        private static readonly Action NoOp = () => { };
    }
}
