using UnityEngine;

namespace Martian.Tooltip
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
