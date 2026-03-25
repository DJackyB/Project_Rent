using UnityEngine;
using Martian.Tooltip;

namespace Martian.Tooltip.Runtime
{
    public interface ITooltipPresenterFactory
    {
        bool CanPresent(TooltipContent content);
        ITooltipPresenter Create(Transform parent);
    }
}
