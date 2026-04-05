using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.UI;
using Martian.Feedback;
using Martian.Feedback.Runtime;
using UnityEngine;

namespace BaoZuPo.Integration.Martian.Feedback
{
    /// <summary>
    /// 反馈适配器。
    /// 将游戏逻辑事件（GameEvents）转换为反馈系统请求（FeedbackRequest/FeedbackSequenceRequest）。
    ///
    /// 主要职责：
    /// - 结算序列反馈：将游戏结算步骤转化为 step-by-step 浮动数字
    /// - 消费/收益反馈：显示卡牌打出的消费、即时收益
    /// - 贷款支付反馈：显示贷款扣款
    /// - 目标定位：根据源（房间、卡牌、全局）确定反馈的锚点和偏移
    ///
    /// 反馈系统关键概念：
    /// - TargetKey：反馈目标的唯一标识（例如 "room:0"、"hud:money"）
    /// - TargetKind：目标类型（Room、Card、Global）
    /// - LaneKey：反馈进度条的键，用于控制多个反馈的并发顺序
    /// - TrackIndex/TrackCount：多序列并排显示时的位置
    /// </summary>
    public static class BaoZuPoFeedbackAdapter
    {
        /// <summary>
        /// 发布结算序列反馈。
        /// 将 SettlementSequenceQueued 转换为 FeedbackSequenceRequest，交由反馈系统 step-by-step 显示。
        /// 返回 FeedbackPlaybackHandle 用于等待动画完成。
        /// </summary>
        public static FeedbackPlaybackHandle PublishSettlementSequence(GameEvents.SettlementSequenceQueued payload, string laneKey = null)
        {
            if (!BaoZuPoMartianFeedbackIntegration.MoneyFeedbackEnabled || payload.Steps == null || payload.Steps.Length == 0)
            {
                return null;
            }

            ResolveSettlementTarget(payload.SourceKind, payload.Room, payload.Card, out var targetKey, out var targetKind, out var anchor, out var useCenterFallback, out var screenOffset);
            screenOffset += ResolveTrackOffset(payload);

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
                    Label = ResolveStepLabel(payload, step),
                    Amount = step.Amount,
                    IsMultiplier = step.IsMultiplier,
                    Category = ResolveStepCategory(step)
                });
            }

            return FeedbackServiceLocator.Current.PublishSequence(request);
        }

        /// <summary>
        /// 发布结算总金额跳跃反馈。
        /// 显示结算批次的总金额变化（从锚点或屏幕中心出现）。
        /// </summary>
        public static FeedbackPlaybackHandle PublishSettlementMoneyJump(string batchId, int totalDelta, RectTransform anchor)
        {
            if (!BaoZuPoMartianFeedbackIntegration.MoneyFeedbackEnabled || totalDelta == 0)
            {
                return null;
            }

            return FeedbackServiceLocator.Current.Publish(new FeedbackRequest
            {
                DebugLabel = string.IsNullOrWhiteSpace(batchId) ? "SettlementMoneyTotal" : $"SettlementMoneyTotal_{batchId}",
                LaneKey = string.IsNullOrWhiteSpace(batchId) ? "settlement-money-total" : $"settlement-money-total:{batchId}",
                TargetKey = "hud:money",
                TargetKind = FeedbackTargetKind.Global,
                Anchor = anchor,
                UseScreenCenterFallback = anchor == null,
                ScreenOffset = new Vector2(0f, 40f),
                Text = FormatSignedAmount(totalDelta),
                NumericDelta = totalDelta,
                Category = totalDelta < 0 ? FeedbackCategory.Cost : FeedbackCategory.Money
            });
        }

        /// <summary>
        /// 发布卡牌打出消费反馈。
        /// 显示卡牌的打出成本（从卡牌或目标房间位置出现）。
        /// </summary>
        public static void PublishPlayCost(CardInstance card, RoomSlot targetRoom, int cost)
        {
            if (!BaoZuPoMartianFeedbackIntegration.MoneyFeedbackEnabled || card == null || cost <= 0)
            {
                return;
            }

            ResolvePlayTarget(card, targetRoom, out var targetKey, out var targetKind, out var anchor, out var useCenterFallback, out var screenOffset);

            FeedbackServiceLocator.Current.Publish(new FeedbackRequest
            {
                DebugLabel = $"{card.Data.cardName}_Cost",
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

        /// <summary>
        /// 发布卡牌即时金币变化反馈。
        /// 显示卡牌打出后立即产生的收益或消费（从卡牌或目标房间位置出现）。
        /// </summary>
        public static void PublishInstantMoneyDelta(CardInstance card, RoomSlot targetRoom, int moneyDelta)
        {
            if (!BaoZuPoMartianFeedbackIntegration.MoneyFeedbackEnabled || card == null || moneyDelta == 0)
            {
                return;
            }

            ResolvePlayTarget(card, targetRoom, out var targetKey, out var targetKind, out var anchor, out var useCenterFallback, out var screenOffset);

            FeedbackServiceLocator.Current.Publish(new FeedbackRequest
            {
                DebugLabel = $"{card.Data.cardName}_InstantMoney",
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

        /// <summary>
        /// 发布贷款支付反馈。
        /// 显示贷款扣款（从屏幕中心或金钱 HUD 出现）。
        /// </summary>
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

        private static string ResolveStepLabel(GameEvents.SettlementSequenceQueued payload, GameEvents.SettlementStep step)
        {
            if (!string.IsNullOrWhiteSpace(step.Label))
            {
                return step.Label;
            }

            if (payload != null && payload.Card != null && payload.Card.Data != null && step.Kind == GameEvents.SettlementStepKind.Delta)
            {
                return CardText.Name(payload.Card);
            }

            return step.Kind switch
            {
                GameEvents.SettlementStepKind.Base => GameText.SettlementBase,
                GameEvents.SettlementStepKind.Delta => GameText.SettlementBonus,
                GameEvents.SettlementStepKind.Multiplier => GameText.SettlementMultiplier,
                GameEvents.SettlementStepKind.Final => GameText.SettlementFinal,
                _ => null
            };
        }

        private static Vector2 ResolveTrackOffset(GameEvents.SettlementSequenceQueued payload)
        {
            if (payload == null || payload.TrackCount <= 1)
            {
                return Vector2.zero;
            }

            float center = (payload.TrackCount - 1) * 0.5f;
            float horizontal = (payload.TrackIndex - center) * 76f;
            return new Vector2(horizontal, 0f);
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
