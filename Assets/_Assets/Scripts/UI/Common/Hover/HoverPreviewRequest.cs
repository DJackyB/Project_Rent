using UnityEngine;

namespace BaoZuPo.UI.Common.Hover
{
    public sealed class HoverPreviewRequest
    {
        public IHoverPreviewSource Source { get; }
        public Vector2 MousePosition { get; }

        public HoverPreviewRequest(IHoverPreviewSource source, Vector2 mousePosition)
        {
            Source = source;
            MousePosition = mousePosition;
        }
    }
}
