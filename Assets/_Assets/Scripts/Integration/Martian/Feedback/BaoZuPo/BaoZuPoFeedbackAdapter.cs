using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.UI;
using BaoZuPo.UI.Common.Animation;
using BaoZuPo.UI.Common.FeedbackPopup;
using UnityEngine;

namespace BaoZuPo.Integration.Martian.Feedback
{
    public static class BaoZuPoFeedbackAdapter
    {
        public static void PublishPlayCost(CardInstance card, RoomSlot targetRoom, int cost)
        {
            if (!BaoZuPoMartianFeedbackIntegration.MoneyFeedbackEnabled || card == null || cost <= 0)
            {
                return;
            }

            var anchor = UIManager.Instance != null ? UIManager.Instance.ResolveMoneyTargetAnchor() : null;
            float verticalGap = UIManager.Instance != null && UIManager.Instance.topBar != null
                ? UIManager.Instance.topBar.PlayCostPopupVerticalGap
                : 18f;

            PublishPopup(
                anchor,
                GameText.FeedbackCost(cost),
                UIFeedbackPopupCategory.Negative,
                ResolveAboveAnchorOffset(anchor, verticalGap),
                anchor == null);
        }

        public static void PublishInstantMoneyDelta(CardInstance card, RoomSlot targetRoom, int moneyDelta)
        {
            if (!BaoZuPoMartianFeedbackIntegration.MoneyFeedbackEnabled || card == null || moneyDelta == 0)
            {
                return;
            }

            ResolvePlayTarget(card, targetRoom, out var anchor, out var useCenterFallback, out var screenOffset);
            PublishPopup(
                anchor,
                FormatSignedAmount(moneyDelta),
                moneyDelta < 0 ? UIFeedbackPopupCategory.Negative : UIFeedbackPopupCategory.Positive,
                screenOffset,
                useCenterFallback);
        }

        public static void PublishLoanPayment(int amount)
        {
            if (!BaoZuPoMartianFeedbackIntegration.MoneyFeedbackEnabled || amount <= 0)
            {
                return;
            }

            var anchor = UIManager.Instance != null ? UIManager.Instance.ResolveMoneyTargetAnchor() : null;
            float verticalGap = UIManager.Instance != null && UIManager.Instance.topBar != null
                ? UIManager.Instance.topBar.PlayCostPopupVerticalGap
                : 18f;

            PublishPopup(
                anchor,
                GameText.FeedbackLoan(amount),
                UIFeedbackPopupCategory.Negative,
                ResolveAboveAnchorOffset(anchor, verticalGap),
                anchor == null);
        }

        private static void ResolvePlayTarget(
            CardInstance card,
            RoomSlot targetRoom,
            out RectTransform anchor,
            out bool useCenterFallback,
            out Vector2 screenOffset)
        {
            if (targetRoom != null)
            {
                anchor = UIManager.Instance != null && UIManager.Instance.boardPanel != null
                    ? UIManager.Instance.boardPanel.ResolveRoomAnchor(targetRoom)
                    : null;
                useCenterFallback = anchor == null;
                screenOffset = new Vector2(0f, 140f);
                return;
            }

            if (card != null && card.Data != null && card.Data.cardType == CardType.Contract)
            {
                anchor = UIManager.Instance != null && UIManager.Instance.boardPanel != null
                    ? UIManager.Instance.boardPanel.ResolveContractAnchor(card)
                    : null;
                useCenterFallback = anchor == null;
                screenOffset = new Vector2(0f, 120f);
                return;
            }

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

        private static void PublishPopup(
            RectTransform anchor,
            string text,
            string category,
            Vector2 screenOffset,
            bool useScreenCenterFallback)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var canvas = UIManager.Instance != null ? UIManager.Instance.GetComponentInParent<Canvas>() : null;
            var layer = UIFeedbackPopupLayer.GetOrCreate(canvas);
            if (layer == null)
            {
                return;
            }

            layer.Show(new UIFeedbackPopupRequest
            {
                Anchor = anchor,
                Text = text,
                Category = category,
                ScreenOffset = screenOffset,
                UseScreenCenterFallback = useScreenCenterFallback,
                AnchorFeedback = PlayPopupAnchorFeedback
            });
        }

        private static void PlayPopupAnchorFeedback(RectTransform anchor)
        {
            UIAnimationTweenUtility.PunchScalePreserveBase(anchor, 0.055f, 0.15f, 6, 0.45f);
        }

        private static Vector2 ResolveAboveAnchorOffset(RectTransform anchor, float verticalGap)
        {
            if (anchor == null)
            {
                return new Vector2(0f, 54f);
            }

            var rect = anchor.rect;
            return new Vector2(
                rect.width * (0.5f - anchor.pivot.x),
                rect.height * (1f - anchor.pivot.y) + verticalGap);
        }
    }
}
