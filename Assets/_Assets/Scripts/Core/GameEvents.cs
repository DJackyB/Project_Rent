using BaoZuPo.Board;
using BaoZuPo.GameFlow;

namespace BaoZuPo.Core
{
    /// <summary>
    /// Shared game and UI event definitions.
    /// </summary>
    public static class GameEvents
    {
        public enum SettlementStepKind
        {
            Base,
            Delta,
            Multiplier,
            Final
        }

        public enum SettlementSourceKind
        {
            Room,
            Contract,
            Event
        }

        public struct SettlementStep
        {
            public SettlementStepKind Kind;
            public string Label;
            public int Amount;
            public bool IsMultiplier;
        }

        public struct MoneyChanged
        {
            public int OldValue;
            public int NewValue;
            public int Delta;
        }

        public struct MoneyInsufficient
        {
            public int CurrentMoney;
            public int RequiredAmount;
        }

        public struct TurnStarted
        {
            public int TurnNumber;
        }

        public struct TurnEnded
        {
            public int TurnNumber;
        }

        public struct PhaseChanged
        {
            public GamePhase Phase;
            public string PhaseName;
        }

        public struct CardPlayed
        {
            public Card.CardInstance Card;
        }

        public struct CardDestroyed
        {
            public Card.CardInstance Card;
            public bool TriggeredByDurability;
        }

        public struct LoanPayment
        {
            public int Amount;
            public int RemainingMoney;
        }

        public struct GameOver
        {
            public int FinalMoney;
            public int TotalTurns;
        }

        public struct GameStateLoaded
        {
            public string SlotId;
            public GamePhase Phase;
            public int TurnNumber;
            public bool IsGameOver;
        }

        public sealed class SettlementSequenceQueued
        {
            public string BatchId;
            public int SourceIndex;
            public int SourceCount;
            public string LaneKey;
            public SettlementSourceKind SourceKind;
            public RoomSlot Room;
            public Card.CardInstance Card;
            public string Title;
            public SettlementStep[] Steps;
            public int FinalAmount;
        }

        public struct SettlementPlaybackCompleted
        {
            public string BatchId;
        }
    }
}
