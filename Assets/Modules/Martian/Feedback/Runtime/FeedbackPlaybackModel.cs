using System.Collections.Generic;
using Martian.Feedback;
using UnityEngine;

namespace Martian.Feedback.Runtime
{
    internal sealed class FeedbackPlaybackRequest
    {
        public string DebugLabel;
        public string LaneKey;
        public string TargetKey;
        public string TargetKind;
        public RectTransform Anchor;
        public bool UseScreenCenterFallback;
        public Vector2 ScreenOffset;
        public float GapSeconds;
        public FeedbackPlaybackHandle Handle;
        public List<FeedbackPlaybackStep> Steps = new();
    }

    internal sealed class FeedbackPlaybackStep
    {
        public string Text;
        public Color Color;
        public float HoldSeconds;
        public float FadeInSeconds;
        public float FadeOutSeconds;
        public float Scale;
        public Vector2 Offset;
        public bool IsFinalStep;
        public string Category;
    }
}
