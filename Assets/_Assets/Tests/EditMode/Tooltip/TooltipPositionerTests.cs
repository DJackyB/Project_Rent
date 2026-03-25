using BaoZuPo.UI.Common.Tooltip.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace BaoZuPo.Tests.UI.Tooltip
{
    public class TooltipPositionerTests
    {
        [Test]
        public void CalculatePointerPosition_ClampsInsideCanvasBounds()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform));
            var tooltipObject = new GameObject("Tooltip", typeof(RectTransform));

            try
            {
                var canvasRect = canvasObject.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(200f, 200f);

                var tooltipRect = tooltipObject.GetComponent<RectTransform>();
                tooltipRect.sizeDelta = new Vector2(100f, 80f);
                tooltipRect.pivot = new Vector2(0f, 1f);

                Vector2 result = TooltipPositioner.CalculatePointerPosition(
                    canvasRect,
                    tooltipRect,
                    new Vector2(500f, 500f),
                    Vector2.zero);

                Assert.LessOrEqual(result.x, 0f);
                Assert.GreaterOrEqual(result.x, -100f);
                Assert.LessOrEqual(result.y, 100f);
                Assert.GreaterOrEqual(result.y, -20f);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
                Object.DestroyImmediate(tooltipObject);
            }
        }

        [Test]
        public void CalculateAnchorPosition_ReturnsClampedValueForAnchorMode()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform));
            var anchorObject = new GameObject("Anchor", typeof(RectTransform));
            var tooltipObject = new GameObject("Tooltip", typeof(RectTransform));

            try
            {
                var canvasRect = canvasObject.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(300f, 200f);

                var anchorRect = anchorObject.GetComponent<RectTransform>();
                anchorRect.SetParent(canvasRect, false);
                anchorRect.anchoredPosition = new Vector2(120f, 80f);
                anchorRect.sizeDelta = new Vector2(40f, 40f);

                var tooltipRect = tooltipObject.GetComponent<RectTransform>();
                tooltipRect.sizeDelta = new Vector2(160f, 90f);
                tooltipRect.pivot = new Vector2(0f, 1f);

                Vector2 result = TooltipPositioner.CalculateAnchorPosition(
                    canvasRect,
                    tooltipRect,
                    anchorRect,
                    new Vector2(12f, -8f));

                Assert.LessOrEqual(result.x, 0f);
                Assert.LessOrEqual(result.y, 100f);
                Assert.GreaterOrEqual(result.x, -150f);
                Assert.GreaterOrEqual(result.y, -10f);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
                Object.DestroyImmediate(anchorObject);
                Object.DestroyImmediate(tooltipObject);
            }
        }
    }
}
