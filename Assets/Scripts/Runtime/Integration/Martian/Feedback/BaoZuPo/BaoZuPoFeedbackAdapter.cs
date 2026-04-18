using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.UI;
using BaoZuPo.UI.Common.Animation;
using BaoZuPo.UI.Common.FeedbackPopup;
using TMPro;
using UnityEngine;

namespace BaoZuPo.Integration.Martian.Feedback
{
    public static class BaoZuPoFeedbackAdapter
    {
        private static readonly Vector2 TurnStartPopupOffset = new(0f, 96f);
        private static readonly Vector2 LoanDueTodayPopupOffset = new(0f, 158f);

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
                anchor == null,
                CreateEmphasizedLoanPaymentStyle(),
                ConfigureEmphasizedLoanPaymentText);
        }

        public static void PublishTurnStartReminders(int turnNumber, int turnsUntilLoanDue, bool isLoanDueToday)
        {
            if (turnNumber <= 0)
            {
                return;
            }

            var canvas = UIManager.Instance != null ? UIManager.Instance.GetComponentInParent<Canvas>() : null;
            var layer = UIFeedbackPopupLayer.GetOrCreate(canvas);
            if (layer == null)
            {
                return;
            }

            int safeTurnsUntilLoanDue = Mathf.Max(0, turnsUntilLoanDue);
            ShowPopupSequence(
                layer,
                CreateTurnStartPopupRequest(null, GameText.TurnStartPopup(turnNumber), TurnStartPopupOffset),
                isLoanDueToday
                    ? CreateLoanDueTodayPopupRequest(null, LoanDueTodayPopupOffset)
                    : CreateTurnStartPopupRequest(null, GameText.LoanDueInTurns(safeTurnsUntilLoanDue), TurnStartPopupOffset + new Vector2(0f, -54f)));
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
            bool useScreenCenterFallback,
            UIFeedbackPopupStyle style = null,
            System.Action<TMP_Text> textConfigurator = null)
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
                Style = style,
                ScreenOffset = screenOffset,
                UseScreenCenterFallback = useScreenCenterFallback,
                TextConfigurator = textConfigurator,
                AnchorFeedback = PlayPopupAnchorFeedback
            });
        }

        private static UIFeedbackPopupRequest CreateTurnStartPopupRequest(RectTransform anchor, string text, Vector2 screenOffset)
        {
            return new UIFeedbackPopupRequest
            {
                Anchor = anchor,
                Text = text,
                Category = UIFeedbackPopupCategory.Default,
                Style = CreateTurnStartPopupStyle(),
                ScreenOffset = screenOffset,
                UseScreenCenterFallback = anchor == null,
                AnchorFeedback = PlayPopupAnchorFeedback
            };
        }

        private static UIFeedbackPopupRequest CreateLoanDueTodayPopupRequest(RectTransform anchor, Vector2 screenOffset)
        {
            return new UIFeedbackPopupRequest
            {
                Anchor = anchor,
                Text = GameText.LoanDueToday,
                Category = UIFeedbackPopupCategory.Negative,
                Style = CreateLoanDueTodayPopupStyle(),
                ScreenOffset = screenOffset,
                UseScreenCenterFallback = anchor == null,
                TextConfigurator = ConfigureLoanDueTodayText,
                AnchorFeedback = PlayPopupAnchorFeedback
            };
        }

        private static void ShowPopupSequence(UIFeedbackPopupLayer layer, params UIFeedbackPopupRequest[] requests)
        {
            if (layer == null || requests == null || requests.Length == 0)
            {
                return;
            }

            int nextIndex = -1;
            for (int i = 0; i < requests.Length; i++)
            {
                if (requests[i] != null)
                {
                    nextIndex = i;
                    break;
                }
            }

            if (nextIndex < 0)
            {
                return;
            }

            ShowPopupSequenceAt(layer, requests, nextIndex);
        }

        private static void ShowPopupSequenceAt(UIFeedbackPopupLayer layer, UIFeedbackPopupRequest[] requests, int index)
        {
            if (layer == null || requests == null || index < 0 || index >= requests.Length || requests[index] == null)
            {
                return;
            }

            requests[index].Completed = () =>
            {
                for (int next = index + 1; next < requests.Length; next++)
                {
                    if (requests[next] == null)
                    {
                        continue;
                    }

                    ShowPopupSequenceAt(layer, requests, next);
                    return;
                }
            };

            layer.Show(requests[index]);
        }

        private static UIFeedbackPopupStyle CreateTurnStartPopupStyle()
        {
            var style = new UIFeedbackPopupStyle
            {
                PanelSize = new Vector2(300f, 78f),
                PanelPadding = new Vector2(28f, 14f),
                FontSize = 32f,
                FinalFontSize = 34f,
                HoldSeconds = 0.58f,
                FadeOutSeconds = 0.14f,
                OvershootScale = 1.2f,
                EntryScale = 0.5f,
                DefaultPanelColor = new Color(0.1f, 0.12f, 0.18f, 0.95f)
            };
            return style;
        }

        private static UIFeedbackPopupStyle CreateLoanDueTodayPopupStyle()
        {
            var style = new UIFeedbackPopupStyle
            {
                PanelSize = new Vector2(340f, 92f),
                PanelPadding = new Vector2(32f, 18f),
                FontSize = 40f,
                FinalFontSize = 42f,
                HoldSeconds = 0.82f,
                FadeOutSeconds = 0.18f,
                OvershootScale = 1.28f,
                EntryScale = 0.46f,
                NegativePanelColor = new Color(0.92f, 0.1f, 0.08f, 0.98f)
            };
            return style;
        }

        private static UIFeedbackPopupStyle CreateEmphasizedLoanPaymentStyle()
        {
            var style = new UIFeedbackPopupStyle
            {
                PanelSize = new Vector2(248f, 62f),
                FontSize = 30f,
                FinalFontSize = 30f,
                HoldSeconds = 0.7f,
                FadeOutSeconds = 0.16f,
                OvershootScale = 1.24f,
                NegativePanelColor = new Color(0.92f, 0.12f, 0.08f, 0.97f)
            };
            return style;
        }

        private static void ConfigureLoanDueTodayText(TMP_Text label)
        {
            if (label == null)
            {
                return;
            }

            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 6f;
        }

        private static void ConfigureEmphasizedLoanPaymentText(TMP_Text label)
        {
            if (label == null)
            {
                return;
            }

            label.fontStyle = FontStyles.Bold;
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
