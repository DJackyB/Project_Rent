using System.Collections.Generic;
using Martian.Feedback;
using UnityEngine;

namespace Martian.Feedback.Runtime
{
    internal static class FeedbackPlaybackFormatting
    {
        private static readonly Vector2 LegacyDefaultScreenOffset = new(0f, 96f);

        public static FeedbackPlaybackRequest Create(FeedbackRuntimeOptions options, FeedbackRequest request)
        {
            if (request == null)
            {
                return null;
            }

            var playback = new FeedbackPlaybackRequest
            {
                DebugLabel = request.DebugLabel,
                LaneKey = request.LaneKey,
                TargetKey = request.TargetKey,
                TargetKind = request.TargetKind,
                Anchor = request.Anchor,
                UseScreenCenterFallback = request.UseScreenCenterFallback,
                ScreenOffset = ResolveScreenOffset(request.ScreenOffset, options != null ? options.DefaultScreenOffset : request.ScreenOffset),
                GapSeconds = 0f
            };

            playback.Steps.Add(new FeedbackPlaybackStep
            {
                Text = !string.IsNullOrWhiteSpace(request.Text) ? request.Text : FormatSignedAmount(request.NumericDelta),
                Color = ResolveColor(options, request.Category, false, false),
                HoldSeconds = options != null ? options.SingleHoldSeconds : 0.6f,
                FadeInSeconds = options != null ? options.SingleFadeInSeconds : 0.12f,
                FadeOutSeconds = options != null ? options.SingleFadeOutSeconds : 0.14f,
                Scale = options != null ? options.SingleScale : 1.05f,
                Offset = Vector2.zero,
                Category = request.Category
            });

            return playback;
        }

        public static FeedbackPlaybackRequest Create(FeedbackRuntimeOptions options, FeedbackSequenceRequest request)
        {
            if (request == null || request.Steps == null || request.Steps.Count == 0)
            {
                return null;
            }

            var playback = new FeedbackPlaybackRequest
            {
                DebugLabel = request.DebugLabel,
                LaneKey = request.LaneKey,
                TargetKey = request.TargetKey,
                TargetKind = request.TargetKind,
                Anchor = request.Anchor,
                UseScreenCenterFallback = request.UseScreenCenterFallback,
                ScreenOffset = ResolveScreenOffset(request.ScreenOffset, options != null ? options.SequenceScreenOffset : request.ScreenOffset),
                GapSeconds = request.GapSeconds > 0f ? request.GapSeconds : (options != null ? options.SequenceGapSeconds : 0.06f)
            };

            int validStepCount = CountValidSteps(request.Steps);
            int resolvedStepIndex = 0;

            for (int i = 0; i < request.Steps.Count; i++)
            {
                var step = request.Steps[i];
                if (step == null)
                {
                    continue;
                }

                bool isFinalStep = resolvedStepIndex == validStepCount - 1;
                playback.Steps.Add(new FeedbackPlaybackStep
                {
                    Text = FormatStep(step),
                    Color = ResolveColor(options, step.Category, isFinalStep, step.IsMultiplier),
                    HoldSeconds = ResolveHoldSeconds(options, step, isFinalStep),
                    FadeInSeconds = options != null ? options.SequenceFadeInSeconds : 0.12f,
                    FadeOutSeconds = options != null ? options.SequenceFadeOutSeconds : 0.14f,
                    Scale = ResolveScale(options, isFinalStep),
                    Offset = step.Offset,
                    IsFinalStep = isFinalStep,
                    Category = step.Category
                });

                resolvedStepIndex++;
            }

            return playback.Steps.Count == 0 ? null : playback;
        }

        public static string FormatSignedAmount(int amount)
        {
            string sign = amount > 0 ? "+" : string.Empty;
            return $"{sign}{amount}";
        }

        private static Vector2 ResolveScreenOffset(Vector2 requestedOffset, Vector2 fallbackOffset)
        {
            if (requestedOffset != Vector2.zero && requestedOffset != LegacyDefaultScreenOffset)
            {
                return requestedOffset;
            }

            return fallbackOffset;
        }

        private static int CountValidSteps(IReadOnlyList<FeedbackStep> steps)
        {
            int count = 0;
            if (steps == null)
            {
                return 0;
            }

            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] != null)
                {
                    count++;
                }
            }

            return count;
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

        private static float ResolveHoldSeconds(FeedbackRuntimeOptions options, FeedbackStep step, bool isFinalStep)
        {
            if (step != null && step.HoldSeconds >= 0f)
            {
                return step.HoldSeconds;
            }

            if (options == null)
            {
                return isFinalStep ? 0.75f : 0.55f;
            }

            return isFinalStep ? options.FinalHoldSeconds : options.NormalHoldSeconds;
        }

        private static float ResolveScale(FeedbackRuntimeOptions options, bool isFinalStep)
        {
            if (options == null)
            {
                return isFinalStep ? 1.08f : 1.05f;
            }

            return isFinalStep ? options.FinalScale : options.SingleScale;
        }

        private static Color ResolveColor(FeedbackRuntimeOptions options, FeedbackCategory category, bool isFinalStep, bool isMultiplier)
        {
            if (options == null)
            {
                if (isFinalStep)
                {
                    return new Color(1f, 0.86f, 0.32f);
                }

                if (isMultiplier)
                {
                    return new Color(0.82f, 0.76f, 1f);
                }

                return category switch
                {
                    FeedbackCategory.Cost => new Color(1f, 0.58f, 0.42f),
                    FeedbackCategory.Loan => new Color(1f, 0.48f, 0.36f),
                    _ => new Color(0.58f, 1f, 0.62f)
                };
            }

            if (isFinalStep)
            {
                return options.FinalColor;
            }

            if (isMultiplier)
            {
                return options.MultiplierColor;
            }

            return category switch
            {
                FeedbackCategory.Cost => options.CostColor,
                FeedbackCategory.Loan => options.LoanColor,
                _ => options.PositiveColor
            };
        }
    }
}
