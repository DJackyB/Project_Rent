using System;
using UnityEngine;

namespace Martian.Feedback
{
    [Serializable]
    public class FeedbackStep
    {
        public string Label;
        public int Amount;
        public bool IsMultiplier;
        public FeedbackCategory Category = FeedbackCategory.Money;
        public float HoldSeconds = -1f;
        public Vector2 Offset = Vector2.zero;
    }
}
