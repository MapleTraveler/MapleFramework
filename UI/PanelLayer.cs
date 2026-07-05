using System.Collections.Generic;
using UnityEngine;

namespace Maple.UI
{
    /// <summary>
    /// 管理所有 Panel 的显示/隐藏。Panel 之间互不影响，可多个并存。
    /// </summary>
    internal sealed class PanelLayer
    {
        internal readonly Transform Root;
        private readonly List<PanelController> _activePanels = new();

        internal PanelLayer(Transform root) => Root = root;

        internal void Show(PanelController panel, object properties)
        {
            if (panel.IsVisible)
            {
                if (properties != null) panel.DoShow(properties, null);
                return;
            }
            _activePanels.Add(panel);
            panel.DoShow(properties, null);
        }

        internal void Hide(PanelController panel)
        {
            if (!panel.IsVisible) return;
            _activePanels.Remove(panel);
            panel.DoHide(null);
        }

        internal void HideAll()
        {
            for (int i = _activePanels.Count - 1; i >= 0; i--)
                _activePanels[i].DoHide(null);
            _activePanels.Clear();
        }

        internal void ClearScoped()
        {
            for (int i = _activePanels.Count - 1; i >= 0; i--)
            {
                if (_activePanels[i].LifeScope == UILifeScope.Scene)
                {
                    _activePanels[i].DoHide(null);
                    _activePanels.RemoveAt(i);
                }
            }
        }
    }
}
