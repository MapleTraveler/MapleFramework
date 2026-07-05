using System.Collections.Generic;
using UnityEngine;

namespace Maple.UI
{
    /// <summary>
    /// 管理所有 Window 的显示/隐藏。以栈结构维护窗口历史：
    /// 打开新窗口时，当前窗口压栈隐藏；关闭当前窗口后，自动恢复栈顶窗口。
    /// </summary>
    internal sealed class WindowLayer
    {
        internal readonly Transform Root;
        private readonly Stack<WindowController> _history = new();
        private WindowController _current;

        internal WindowLayer(Transform root) => Root = root;

        internal WindowController Current => _current;

        internal void Show(WindowController window, object properties)
        {
            if (_current == window)
            {
                if (properties != null) window.DoShow(properties, null);
                return;
            }

            if (_current != null)
            {
                _history.Push(_current);
                _current.DoHide(null);
            }

            _current = window;
            window.DoShow(properties, null);
        }

        internal void Hide(WindowController window)
        {
            if (_current != window) return;

            _current.DoHide(() =>
            {
                _current = null;
                if (_history.Count > 0)
                {
                    _current = _history.Pop();
                    _current.DoShow(null, null);
                }
            });
        }

        internal void CloseAll()
        {
            if (_current != null)
            {
                _current.DoHide(null);
                _current = null;
            }
            while (_history.Count > 0)
            {
                var w = _history.Pop();
                if (w.IsVisible) w.DoHide(null);
            }
        }

        internal void ClearScoped()
        {
            var kept = new Stack<WindowController>();
            while (_history.Count > 0)
            {
                var w = _history.Pop();
                if (w.LifeScope == UILifeScope.Scene)
                {
                    if (w.IsVisible) w.DoHide(null);
                }
                else
                {
                    kept.Push(w);
                }
            }
            while (kept.Count > 0)
                _history.Push(kept.Pop());

            if (_current != null && _current.LifeScope == UILifeScope.Scene)
            {
                _current.DoHide(null);
                _current = _history.Count > 0 ? _history.Pop() : null;
                _current?.DoShow(null, null);
            }
        }
    }
}
