using UnityEngine;
using Martian.Tooltip;

namespace Martian.Tooltip.Runtime
{
    public interface ITooltipPresenter
    {
        RectTransform Root { get; }

        void Show(TooltipRequest request);
        void Hide();
    }
}
