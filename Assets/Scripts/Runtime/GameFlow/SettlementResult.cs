using System;
using System.Collections.Generic;
using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;

namespace BaoZuPo.GameFlow
{
    public enum SettlementPlaybackStageKind
    {
        Serial,
        Parallel,
        Barrier,
        Aggregate
    }

    public sealed class SettlementRequest
    {
        public int CurrentTurn { get; }
        public int LoanPaymentCount { get; }
        public Action<IReadOnlyList<CardInstance>> BeforeHandWaitCardsRemoved { get; }

        public SettlementRequest(
            int currentTurn,
            int loanPaymentCount,
            Action<IReadOnlyList<CardInstance>> beforeHandWaitCardsRemoved = null)
        {
            CurrentTurn = currentTurn;
            LoanPaymentCount = loanPaymentCount;
            BeforeHandWaitCardsRemoved = beforeHandWaitCardsRemoved;
        }
    }

    public sealed class SettlementResult
    {
        public string SettlementId { get; set; }
        public int TurnNumber { get; set; }
        public int MoneyBefore { get; set; }
        public int MoneyAfter { get; set; }
        public int NewLoanPaymentCount { get; set; }
        public bool IsGameOver { get; set; }
        public bool RewardBoosted { get; set; }
        public List<SettlementStageResult> Stages { get; } = new();
        public List<CardDurabilityChange> DurabilityChanges { get; } = new();
        public List<CardDestroyedResult> DestroyedCards { get; } = new();
        public HandWaitCleanupResult HandCleanup { get; set; } = new();
        public LoanSettlementResult Loan { get; set; } = new();
        public GameOverResult GameOver { get; set; } = new();

        public bool IsEmpty => Stages.Count == 0;
        public int TotalDelta => MoneyAfter - MoneyBefore;
    }

    public sealed class SettlementStageResult
    {
        public string DebugLabel { get; set; }
        public SettlementPlaybackStageKind Kind { get; set; } = SettlementPlaybackStageKind.Serial;
        public List<SettlementSourceResult> Sources { get; } = new();
    }

    public sealed class SettlementSourceResult
    {
        public int SourceIndex { get; set; }
        public GameEvents.SettlementSourceKind SourceKind { get; set; }
        public RoomSlot Room { get; set; }
        public CardInstance Card { get; set; }
        public string Title { get; set; }
        public GameEvents.SettlementStep[] Steps { get; set; } = Array.Empty<GameEvents.SettlementStep>();
        public int FinalAmount { get; set; }
    }

    public sealed class CardDurabilityChange
    {
        public CardInstance Card { get; set; }
        public RoomSlot Room { get; set; }
        public int Before { get; set; }
        public int After { get; set; }
        public bool MarkedForDestroy { get; set; }
    }

    public sealed class CardDestroyedResult
    {
        public CardInstance Card { get; set; }
        public bool TriggeredByDurability { get; set; }
        public int MoneyBeforeDestroyEffect { get; set; }
        public int MoneyAfterDestroyEffect { get; set; }
    }

    public sealed class HandWaitCleanupResult
    {
        public List<CardInstance> CardsDiscarded { get; } = new();
        public int RemovedCount => CardsDiscarded.Count;
    }

    public sealed class LoanSettlementResult
    {
        public bool IsDue { get; set; }
        public int Amount { get; set; }
        public bool Paid { get; set; }
        public int RemainingMoney { get; set; }
        public int PaymentIndexBefore { get; set; }
        public int PaymentIndexAfter { get; set; }
    }

    public sealed class GameOverResult
    {
        public bool IsGameOver { get; set; }
        public int FinalMoney { get; set; }
        public int TotalTurns { get; set; }
        public string Reason { get; set; }
    }
}
