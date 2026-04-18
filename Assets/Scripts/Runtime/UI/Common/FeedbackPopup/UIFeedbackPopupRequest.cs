using System;
using TMPro;
using UnityEngine;

namespace BaoZuPo.UI.Common.FeedbackPopup
{
    public sealed class UIFeedbackPopupRequest
    {
        public RectTransform Anchor;
        public Vector2 ScreenOffset = new Vector2(0f, 72f);
        public bool UseScreenCenterFallback = true;
        public string Text;
        public string Category = UIFeedbackPopupCategory.Default;
        public bool IsFinal;
        public UIFeedbackPopupStyle Style;
        public Action<TMP_Text> TextConfigurator;
        public Action<RectTransform> AnchorFeedback;
        public Action Completed;
    }
}
