using UnityEngine;

namespace BaoZuPo.UI.Common.Hover
{
    /// <summary>
    /// 预览窗口定位工具，计算并夹紧悬停预览的位置使其保持在画布范围内。
    /// 考虑屏幕空间和画布坐标转换，防止预览超出屏幕边界。
    /// </summary>
    public static class HoverPreviewPositioner
    {
        public static Vector2 CalculateClampedPosition(RectTransform canvasRect, RectTransform previewRect, Vector2 mousePosition, Vector2 offset)
        {
            if (canvasRect == null || previewRect == null)
            {
                return mousePosition + offset;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePosition, null, out var localPoint);
            Vector2 target = localPoint + offset;

            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 previewSize = previewRect.rect.size;
            Vector2 pivot = previewRect.pivot;

            float minX = -canvasSize.x * 0.5f + previewSize.x * pivot.x;
            float maxX = canvasSize.x * 0.5f - previewSize.x * (1f - pivot.x);
            float minY = -canvasSize.y * 0.5f + previewSize.y * pivot.y;
            float maxY = canvasSize.y * 0.5f - previewSize.y * (1f - pivot.y);

            target.x = Mathf.Clamp(target.x, minX, maxX);
            target.y = Mathf.Clamp(target.y, minY, maxY);
            return target;
        }
    }
}
