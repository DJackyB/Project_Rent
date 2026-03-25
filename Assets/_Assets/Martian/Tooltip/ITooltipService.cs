using UnityEngine;

namespace Martian.Tooltip
{
    public interface ITooltipService
    {
        bool IsAvailable { get; }

        void Show(TooltipRequest request, Vector2? pointerPosition = null);
        void Hide(object owner);
        void HideAll();
    }
}
