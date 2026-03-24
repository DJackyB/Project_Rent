using UnityEngine;

namespace BaoZuPo.UI.Common.Hover
{
    public interface IHoverPreviewPresenter
    {
        RectTransform Root { get; }

        void Show(HoverPreviewRequest request);
        void Hide();
    }
}
