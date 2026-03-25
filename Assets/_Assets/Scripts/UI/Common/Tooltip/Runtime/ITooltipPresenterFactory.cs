using UnityEngine;
using BaoZuPo.UI.Common.Tooltip;

namespace BaoZuPo.UI.Common.Tooltip.Runtime
{
    public interface ITooltipPresenterFactory
    {
        bool CanPresent(TooltipContent content);
        ITooltipPresenter Create(Transform parent);
    }
}
