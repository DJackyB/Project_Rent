using BaoZuPo.Board;
using BaoZuPo.GameFlow;
using UnityEngine;

namespace BaoZuPo.Core
{
    /// <summary>
    /// Central event definitions shared by gameplay and UI systems.
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
            public Card.CardInstance SourceCard;
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

        public sealed class SettlementSequenceQueued
        {
            public string BatchId;
            public int SourceIndex;
            public int SourceCount;
            public int TrackIndex;
            public int TrackCount = 1;
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

        public struct CardRewardOffered
        {
            public Card.CardData[] Options;
            public bool Boosted;
        }

        public struct CardRewardSelected
        {
            public Card.CardData ChosenCard;
            public bool HasSourceWorldPosition;
            public Vector3 SourceWorldPosition;
        }

        public struct ShopOpened
        {
            public Card.CardData[] Options;
        }

        public struct ShopClosed
        {
            public int TurnNumber;
        }
    }
}
