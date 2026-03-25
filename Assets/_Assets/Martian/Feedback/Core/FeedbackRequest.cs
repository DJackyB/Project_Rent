using System;
using UnityEngine;

namespace Martian.Feedback
{
    [Serializable]
    public class FeedbackRequest
    {
        public string DebugLabel;
        public string TargetKey;
        public FeedbackTargetKind TargetKind = FeedbackTargetKind.Global;
        public FeedbackChannel Channel = FeedbackChannel.FloatingText;
        public RectTransform Anchor;
        public bool UseScreenCenterFallback = true;
        public Vector2 ScreenOffset = new Vector2(0f, 96f);
        public string Text;
        public int NumericDelta;
        public FeedbackCategory Category = FeedbackCategory.Money;
        public int Priority;
    }
}
