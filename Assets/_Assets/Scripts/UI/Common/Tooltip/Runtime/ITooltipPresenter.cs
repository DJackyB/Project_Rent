using UnityEngine;
using BaoZuPo.UI.Common.Tooltip;

namespace BaoZuPo.UI.Common.Tooltip.Runtime
{
    public interface ITooltipPresenter
    {
        RectTransform Root { get; }

        void Show(TooltipRequest request);
        void Hide();
    }
}
