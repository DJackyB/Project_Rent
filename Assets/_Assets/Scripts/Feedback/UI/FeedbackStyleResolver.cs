using BaoZuPo.Feedback.Core;
using BaoZuPo.UI.Common.Sequence;
using UnityEngine;

namespace BaoZuPo.Feedback.UI
{
    public static class FeedbackStyleResolver
    {
        private static readonly Color PositiveColor = new(0.58f, 1f, 0.62f);
        private static readonly Color CostColor = new(1f, 0.58f, 0.42f);
        private static readonly Color LoanColor = new(1f, 0.48f, 0.36f);
        private static readonly Color FinalColor = new(1f, 0.86f, 0.32f);
        private static readonly Color MultiplierColor = new(0.82f, 0.76f, 1f);

        public static UISequencePlaybackRequest BuildPlaybackRequest(FeedbackSequenceRequest request)
        {
            if (request == null)
            {
                return null;
            }

            var playbackRequest = new UISequencePlaybackRequest
            {
                DebugLabel = request.DebugLabel,
                Anchor = request.Anchor,
                UseScreenCenterFallback = request.UseScreenCenterFallback,
                ScreenOffset = request.ScreenOffset,
                GapSeconds = request.GapSeconds
            };

            int validStepCount = 0;
            if (request.Steps != null)
            {
                for (int i = 0; i < request.Steps.Count; i++)
                {
                    if (request.Steps[i] != null)
                    {
                        validStepCount++;
                    }
                }
            }

            int resolvedStepIndex = 0;
            if (request.Steps != null)
            {
                for (int i = 0; i < request.Steps.Count; i++)
                {
                    var step = request.Steps[i];
                    if (step == null)
                    {
                        continue;
                    }

                    bool isFinalStep = resolvedStepIndex == validStepCount - 1;
                    playbackRequest.Steps.Add(new UISequenceStep
                    {
                        Text = FormatStep(step),
                        Color = ResolveColor(step, isFinalStep),
                        HoldSeconds = step.HoldSeconds >= 0f ? step.HoldSeconds : isFinalStep ? 0.75f : 0.55f,
                        FadeInSeconds = 0.12f,
                        FadeOutSeconds = 0.14f,
                        Scale = isFinalStep ? 1.08f : 1f,
                        Offset = step.Offset
                    });

                    resolvedStepIndex++;
                }
            }

            return playbackRequest;
        }

        public static UISequencePlaybackRequest BuildPlaybackRequest(FeedbackRequest request)
        {
            if (request == null)
            {
                return null;
            }

            var playbackRequest = new UISequencePlaybackRequest
            {
                DebugLabel = request.DebugLabel,
                Anchor = request.Anchor,
                UseScreenCenterFallback = request.UseScreenCenterFallback,
                ScreenOffset = request.ScreenOffset,
                GapSeconds = 0f
            };

            playbackRequest.Steps.Add(new UISequenceStep
            {
                Text = !string.IsNullOrWhiteSpace(request.Text) ? request.Text : FormatSignedAmount(request.NumericDelta),
                Color = ResolveColor(new FeedbackStep { Category = request.Category }, isFinalStep: false),
                HoldSeconds = 0.6f,
                FadeInSeconds = 0.12f,
                FadeOutSeconds = 0.14f,
                Scale = 1.05f,
                Offset = Vector2.zero
            });

            return playbackRequest;
        }

        private static string FormatStep(FeedbackStep step)
        {
            if (step == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(step.Label))
            {
                if (step.IsMultiplier)
                {
                    float multiplier = step.Amount / 100f;
                    return $"{step.Label} x{multiplier:0.##}";
                }

                if (step.Label.Contains("{0}"))
                {
                    return string.Format(step.Label, FormatSignedAmount(step.Amount));
                }

                return $"{step.Label} {FormatSignedAmount(step.Amount)}";
            }

            if (step.IsMultiplier)
            {
                float multiplier = step.Amount / 100f;
                return $"x{multiplier:0.##}";
            }

            return FormatSignedAmount(step.Amount);
        }

        private static string FormatSignedAmount(int amount)
        {
            string sign = amount > 0 ? "+" : string.Empty;
            return $"{sign}{amount}";
        }

        private static Color ResolveColor(FeedbackStep step, bool isFinalStep)
        {
            if (isFinalStep)
            {
                return FinalColor;
            }

            if (step != null && step.IsMultiplier)
            {
                return MultiplierColor;
            }

            return step != null ? step.Category switch
            {
                FeedbackCategory.Cost => CostColor,
                FeedbackCategory.Loan => LoanColor,
                _ => PositiveColor
            } : Color.white;
        }
    }
}
