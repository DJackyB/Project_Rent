using System;
using System.Collections.Generic;
using BaoZuPo.Board;
using BaoZuPo.Core;
using BaoZuPo.Deck;
using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// Additional execution context used while card effects run.
    /// </summary>
    public class EffectExecutionContext
    {
        /// <summary>Currently selected room target, or null.</summary>
        public RoomSlot SelectedRoom { get; set; }

        /// <summary>Optional settlement capture hook used during settle phase.</summary>
        public SettlementCaptureContext SettlementCapture { get; set; }

        /// <summary>
        /// Guards extra room settlement so TriggerSelectedRoomSettle cannot recurse into itself.
        /// </summary>
        public bool IsExtraRoomSettlementActive { get; set; }
    }

    /// <summary>
    /// Card effect interface.
    /// </summary>
    public interface ICardEffect
    {
        void Execute(CardInstance source, GameContext context);
    }

    /// <summary>
    /// Shared game context used while effects execute.
    /// </summary>
    public class GameContext
    {
        public Economy.MoneyManager MoneyManager { get; set; }
        public Board.BoardManager BoardManager { get; set; }
        public DeckManager DeckManager { get; set; }
        public SettlementCaptureContext SettlementCapture { get; }
        public EffectExecutionContext EffectContext { get; }

        public GameContext()
        {
            SettlementCapture = new SettlementCaptureContext();
            EffectContext = new EffectExecutionContext
            {
                SettlementCapture = SettlementCapture
            };
        }
    }

    /// <summary>
    /// Captures ordered money steps while still letting the real money state update immediately.
    /// </summary>
    public sealed class SettlementCaptureContext
    {
        private readonly List<GameEvents.SettlementStep> _steps = new();

        public bool IsCapturing { get; private set; }
        public IReadOnlyList<GameEvents.SettlementStep> Steps => _steps;

        public void Begin()
        {
            _steps.Clear();
            IsCapturing = true;
        }

        public void RecordBase(int amount, string label = null)
        {
            RecordStep(GameEvents.SettlementStepKind.Base, amount, false, label);
        }

        public void RecordDelta(int amount, string label = null)
        {
            RecordStep(GameEvents.SettlementStepKind.Delta, amount, false, label);
        }

        public void RecordMultiplier(float multiplier, string label = null)
        {
            int amount = Mathf.RoundToInt(multiplier * 100f);
            RecordStep(GameEvents.SettlementStepKind.Multiplier, amount, true, label);
        }

        public GameEvents.SettlementStep[] Complete(int finalAmount, string label = null, bool includeFinalStep = true)
        {
            if (includeFinalStep)
            {
                RecordStep(GameEvents.SettlementStepKind.Final, finalAmount, false, label);
            }

            IsCapturing = false;
            return _steps.ToArray();
        }

        public void Reset()
        {
            _steps.Clear();
            IsCapturing = false;
        }

        private void RecordStep(GameEvents.SettlementStepKind kind, int amount, bool isMultiplier, string label)
        {
            if (!IsCapturing)
            {
                return;
            }

            if (kind != GameEvents.SettlementStepKind.Final && amount == 0)
            {
                return;
            }

            _steps.Add(new GameEvents.SettlementStep
            {
                Kind = kind,
                Label = string.IsNullOrWhiteSpace(label) ? null : label,
                Amount = amount,
                IsMultiplier = isMultiplier
            });
        }
    }
}
