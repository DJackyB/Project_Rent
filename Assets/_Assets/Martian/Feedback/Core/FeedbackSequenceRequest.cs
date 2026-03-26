using System;
using System.Collections.Generic;
using UnityEngine;

namespace Martian.Feedback
{
    [Serializable]
    public class FeedbackSequenceRequest
    {
        public string SequenceId;
        public string DebugLabel;
        public string LaneKey;
        public string TargetKey;
        public FeedbackTargetKind TargetKind = FeedbackTargetKind.Global;
        public FeedbackChannel Channel = FeedbackChannel.Sequence;
        public RectTransform Anchor;
        public bool UseScreenCenterFallback = true;
        public Vector2 ScreenOffset = new Vector2(0f, 96f);
        public float GapSeconds = 0.06f;
        public List<FeedbackStep> Steps = new();
    }
}
