using UnityEngine;

namespace BaoZuPo.UI.Common.Tooltip
{
    public sealed class NullTooltipService : ITooltipService
    {
        public bool IsAvailable => false;

        public void Show(TooltipRequest request, Vector2? pointerPosition = null)
        {
        }

        public void Hide(object owner)
        {
        }

        public void HideAll()
        {
        }
    }
}
