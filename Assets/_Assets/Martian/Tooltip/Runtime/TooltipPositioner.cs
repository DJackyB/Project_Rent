using UnityEngine;

namespace Martian.Tooltip.Runtime
{
    public static class TooltipPositioner
    {
        public static Vector2 CalculatePointerPosition(
            RectTransform canvasRect,
            RectTransform tooltipRect,
            Vector2 screenPoint,
            Vector2 offset,
            Camera eventCamera = null)
        {
            if (canvasRect == null || tooltipRect == null)
            {
                return screenPoint + offset;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, eventCamera, out var localPoint);
            return ClampLocalPosition(canvasRect, tooltipRect, localPoint + offset);
        }

        public static Vector2 CalculateAnchorPosition(
            RectTransform canvasRect,
            RectTransform tooltipRect,
            RectTransform anchorRect,
            Vector2 offset,
            Camera eventCamera = null)
        {
            if (anchorRect == null)
            {
                return CalculatePointerPosition(canvasRect, tooltipRect, offset, Vector2.zero, eventCamera);
            }

            var worldCorners = new Vector3[4];
            anchorRect.GetWorldCorners(worldCorners);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCorners[2]);
            return CalculatePointerPosition(canvasRect, tooltipRect, topRight, offset, eventCamera);
        }

        private static Vector2 ClampLocalPosition(RectTransform canvasRect, RectTransform tooltipRect, Vector2 localPosition)
        {
            Vector2 canvasSize = canvasRect.rect.size;
            // 乘以 localScale 以获取实际视觉尺寸（支持 localScale 缩放的 tooltip）
            Vector3 scale = tooltipRect.localScale;
            Vector2 tooltipSize = new Vector2(
                tooltipRect.rect.size.x * Mathf.Abs(scale.x),
                tooltipRect.rect.size.y * Mathf.Abs(scale.y));
            Vector2 pivot = tooltipRect.pivot;

            float minX = -canvasSize.x * 0.5f + tooltipSize.x * pivot.x;
            float maxX = canvasSize.x * 0.5f - tooltipSize.x * (1f - pivot.x);
            float minY = -canvasSize.y * 0.5f + tooltipSize.y * pivot.y;
            float maxY = canvasSize.y * 0.5f - tooltipSize.y * (1f - pivot.y);

            localPosition.x = Mathf.Clamp(localPosition.x, minX, maxX);
            localPosition.y = Mathf.Clamp(localPosition.y, minY, maxY);
            return localPosition;
        }

    }
}
