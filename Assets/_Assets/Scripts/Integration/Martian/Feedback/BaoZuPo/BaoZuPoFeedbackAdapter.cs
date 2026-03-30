using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.Localization;
using BaoZuPo.UI;
using Martian.Feedback;
using Martian.Feedback.Runtime;
using UnityEngine;

namespace BaoZuPo.Integration.Martian.Feedback
{
    public static class BaoZuPoFeedbackAdapter
    {
        public static FeedbackPlaybackHandle PublishSettlementSequence(GameEvents.SettlementSequenceQueued payload, string laneKey = null)
        {
            if (!BaoZuPoMartianFeedbackIntegration.MoneyFeedbackEnabled || payload.Steps == null || payload.Steps.Length == 0)
            {
                return null;
            }

            ResolveSettlementTarget(payload.SourceKind, payload.Room, payload.Card, out var targetKey, out var targetKind, out var anchor, out var useCenterFallback, out var screenOffset);

            var request = new FeedbackSequenceRequest
            {
                SequenceId = $"settlement:{payload.BatchId}:{payload.SourceKind}:{payload.SourceIndex}:{targetKey}",
                DebugLabel = payload.Title,
                LaneKey = !string.IsNullOrWhiteSpace(laneKey)
                    ? laneKey
                    : !string.IsNullOrWhiteSpace(payload.LaneKey)
                        ? payload.LaneKey
                        : "settlement-global",
                TargetKey = targetKey,
                TargetKind = targetKind,
                Anchor = anchor,
                UseScreenCenterFallback = useCenterFallback,
                ScreenOffset = screenOffset,
                GapSeconds = 0.06f
            };

            for (int i = 0; i < payload.Steps.Length; i++)
            {
                var step = payload.Steps[i];
                request.Steps.Add(new FeedbackStep
                {
                    Label = step.Label,
                    Amount = step.Amount,
                    IsMultiplier = step.IsMultiplier,
                    Category = ResolveStepCategory(step)
                });
            }

            return FeedbackServiceLocator.Current.PublishSequence(request);
        }

        public static void PublishPlayCost(CardInstance card, RoomSlot targetRoom, int cost)
        {
            if (!BaoZuPoMartianFeedbackIntegration.MoneyFeedbackEnabled || card == null || cost <= 0)
            {
                return;
            }

            ResolvePlayTarget(card, targetRoom, out var targetKey, out var targetKind, out var anchor, out var useCenterFallback, out var screenOffset);

            FeedbackServiceLocator.Current.Publish(new FeedbackRequest
            {
                DebugLabel = $"{CardTextResolver.ResolveName(card)}_Cost",
                TargetKey = targetKey,
                TargetKind = targetKind,
                Anchor = anchor,
                UseScreenCenterFallback = useCenterFallback,
                ScreenOffset = screenOffset,
                Text = GameText.FeedbackCost(cost),
                NumericDelta = -cost,
                Category = FeedbackCategory.Cost
            });
        }

        public static void PublishInstantMoneyDelta(CardInstance card, RoomSlot targetRoom, int moneyDelta)
        {
            if (!BaoZuPoMartianFeedbackIntegration.MoneyFeedbackEnabled || card == null || moneyDelta == 0)
            {
                return;
            }

            ResolvePlayTarget(card, targetRoom, out var targetKey, out var targetKind, out var anchor, out var useCenterFallback, out var screenOffset);

            FeedbackServiceLocator.Current.Publish(new FeedbackRequest
            {
                DebugLabel = $"{CardTextResolver.ResolveName(card)}_InstantMoney",
                TargetKey = targetKey,
                TargetKind = targetKind,
                Anchor = anchor,
                UseScreenCenterFallback = useCenterFallback,
                ScreenOffset = screenOffset,
                Text = FormatSignedAmount(moneyDelta),
                NumericDelta = moneyDelta,
                Category = moneyDelta < 0 ? FeedbackCategory.Cost : FeedbackCategory.Money
            });
        }

        public static void PublishLoanPayment(int amount)
        {
            if (!BaoZuPoMartianFeedbackIntegration.MoneyFeedbackEnabled || amount <= 0)
            {
                return;
            }

            ResolveGlobalTarget("loan", out var targetKey, out var anchor, out var useCenterFallback, out var screenOffset);

            FeedbackServiceLocator.Current.Publish(new FeedbackRequest
            {
                DebugLabel = "LoanPayment",
                TargetKey = targetKey,
                TargetKind = FeedbackTargetKind.Global,
                Anchor = anchor,
                UseScreenCenterFallback = useCenterFallback,
                ScreenOffset = screenOffset,
                Text = GameText.FeedbackLoan(amount),
                NumericDelta = -amount,
                Category = FeedbackCategory.Loan
            });
        }

        private static FeedbackCategory ResolveStepCategory(GameEvents.SettlementStep step)
        {
            if (step.IsMultiplier)
            {
                return FeedbackCategory.Settlement;
            }

            return step.Amount < 0 ? FeedbackCategory.Cost : FeedbackCategory.Money;
        }

        private static void ResolveSettlementTarget(
            GameEvents.SettlementSourceKind sourceKind,
            RoomSlot room,
            CardInstance card,
            out string targetKey,
            out FeedbackTargetKind targetKind,
            out RectTransform anchor,
            out bool useCenterFallback,
            out Vector2 screenOffset)
        {
            switch (sourceKind)
            {
                case GameEvents.SettlementSourceKind.Room when room != null:
                    targetKey = $"room:{room.RoomIndex}";
                    targetKind = FeedbackTargetKind.Room;
                    anchor = UIManager.Instance != null && UIManager.Instance.boardPanel != null
                        ? UIManager.Instance.boardPanel.ResolveRoomAnchor(room)
                        : null;
                    useCenterFallback = anchor == null;
                    screenOffset = new Vector2(0f, 140f);
                    return;
                case GameEvents.SettlementSourceKind.Contract when card != null:
                    targetKey = $"card:{card.GetHashCode()}";
                    targetKind = FeedbackTargetKind.Card;
                    anchor = UIManager.Instance != null && UIManager.Instance.boardPanel != null
                        ? UIManager.Instance.boardPanel.ResolveContractAnchor(card)
                        : null;
                    useCenterFallback = anchor == null;
                    screenOffset = new Vector2(0f, 120f);
                    return;
                default:
                    ResolveGlobalTarget(card != null ? $"event:{card.GetHashCode()}" : "event", out targetKey, out anchor, out useCenterFallback, out screenOffset);
                    targetKind = FeedbackTargetKind.Global;
                    return;
            }
        }

        private static void ResolvePlayTarget(
            CardInstance card,
            RoomSlot targetRoom,
            out string targetKey,
            out FeedbackTargetKind targetKind,
            out RectTransform anchor,
            out bool useCenterFallback,
            out Vector2 screenOffset)
        {
            if (targetRoom != null)
            {
                targetKey = $"room:{targetRoom.RoomIndex}";
                targetKind = FeedbackTargetKind.Room;
                anchor = UIManager.Instance != null && UIManager.Instance.boardPanel != null
                    ? UIManager.Instance.boardPanel.ResolveRoomAnchor(targetRoom)
                    : null;
                useCenterFallback = anchor == null;
                screenOffset = new Vector2(0f, 140f);
                return;
            }

            if (card != null && card.Data != null && card.Data.cardType == CardType.Contract)
            {
                targetKey = $"card:{card.GetHashCode()}";
                targetKind = FeedbackTargetKind.Card;
                anchor = UIManager.Instance != null && UIManager.Instance.boardPanel != null
                    ? UIManager.Instance.boardPanel.ResolveContractAnchor(card)
                    : null;
                useCenterFallback = anchor == null;
                screenOffset = new Vector2(0f, 120f);
                return;
            }

            ResolveGlobalTarget(card != null ? $"play:{card.GetHashCode()}" : "play", out targetKey, out anchor, out useCenterFallback, out screenOffset);
            targetKind = FeedbackTargetKind.Global;
        }

        private static void ResolveGlobalTarget(
            string keySuffix,
            out string targetKey,
            out RectTransform anchor,
            out bool useCenterFallback,
            out Vector2 screenOffset)
        {
            targetKey = $"global:{keySuffix}";
            anchor = UIManager.Instance != null && UIManager.Instance.boardPanel != null
                ? UIManager.Instance.boardPanel.ResolvePlayAreaAnchor()
                : null;
            useCenterFallback = anchor == null;
            screenOffset = new Vector2(0f, 48f);
        }

        private static string FormatSignedAmount(int amount)
        {
            string sign = amount > 0 ? "+" : string.Empty;
            return $"{sign}{amount}";
        }
    }
}
