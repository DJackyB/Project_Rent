using Martian.Tooltip.Runtime;
using UnityEngine;

namespace Martian.Tooltip.Presets
{
    public sealed class TooltipDocumentPresenterFactory : ITooltipPresenterFactory
    {
        public bool CanPresent(TooltipContent content)
        {
            return content != null
                && content.ContentId == TooltipDocumentContentIds.Document
                && content.Payload is TooltipDocument;
        }

        public ITooltipPresenter Create(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            var presenterObject = new GameObject(nameof(TooltipDocumentPresenter), typeof(RectTransform), typeof(TooltipDocumentPresenter));
            presenterObject.transform.SetParent(parent, false);
            return presenterObject.GetComponent<TooltipDocumentPresenter>();
        }
    }
}
