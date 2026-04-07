using Martian.Feedback;
using UnityEngine;

namespace Martian.Feedback.Presets
{
    public static class FeedbackPresets
    {
        public static FeedbackRequest SignedNumber(string targetKey, int amount, string category = FeedbackCategories.Default, RectTransform anchor = null, string debugLabel = null)
        {
            return new FeedbackRequest
            {
                TargetKey = targetKey,
                Anchor = anchor,
                DebugLabel = debugLabel,
                NumericDelta = amount,
                Category = category,
                Text = amount > 0 ? $"+{amount}" : amount.ToString()
            };
        }

        public static FeedbackStep Step(string label, int amount, string category = FeedbackCategories.Default, bool isMultiplier = false, float holdSeconds = -1f, Vector2 offset = default)
        {
            return new FeedbackStep
            {
                Label = label,
                Amount = amount,
                Category = category,
                IsMultiplier = isMultiplier,
                HoldSeconds = holdSeconds,
                Offset = offset
            };
        }

        public static FeedbackSequenceRequest Sequence(string targetKey, params FeedbackStep[] steps)
        {
            var request = new FeedbackSequenceRequest
            {
                TargetKey = targetKey
            };

            if (steps != null)
            {
                for (int i = 0; i < steps.Length; i++)
                {
                    if (steps[i] != null)
                    {
                        request.Steps.Add(steps[i]);
                    }
                }
            }

            return request;
        }
    }
}
