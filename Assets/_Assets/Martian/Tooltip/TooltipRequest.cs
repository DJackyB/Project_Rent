using UnityEngine;

namespace Martian.Tooltip
{
    public sealed class TooltipRequest
    {
        public object Owner { get; }
        public RectTransform Anchor { get; }
        public TooltipContent Content { get; }
        public TooltipPlacementMode PlacementMode { get; }
        public Vector2 Offset { get; }

        public TooltipRequest(
            object owner,
            RectTransform anchor,
            TooltipContent content,
            TooltipPlacementMode placementMode = TooltipPlacementMode.FollowPointer,
            Vector2? offset = null)
        {
            Owner = owner ?? anchor;
            Anchor = anchor;
            Content = content;
            PlacementMode = placementMode;
            Offset = offset ?? Vector2.zero;
        }
    }
}
