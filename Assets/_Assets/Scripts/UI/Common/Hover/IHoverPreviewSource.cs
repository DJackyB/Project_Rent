using UnityEngine;

namespace BaoZuPo.UI.Common.Hover
{
    public interface IHoverPreviewSource
    {
        GameObject HoverSourceObject { get; }
        RectTransform HoverAnchor { get; }
        object HoverPayload { get; }
    }
}
