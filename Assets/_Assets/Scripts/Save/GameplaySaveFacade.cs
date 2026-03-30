using System;
using System.Collections.Generic;
using System.IO;
using BaoZuPo.Core;
using BaoZuPo.GameFlow;
using BaoZuPo.UI;
using Martian.EventBus;
using Martian.Save;
using Martian.Save.Runtime;
using UnityEngine;

namespace BaoZuPo.Save
{
    public sealed class GameplaySaveFacade
    {
        public const string AutoSaveSlotId = "autosave";
        public const string AutoSaveDisplayName = "Auto Save";

        private readonly SaveService _saveService;
        private readonly string _relativeDirectory;

        public GameplaySaveFacade(SaveService saveService, string relativeDirectory = "gameplay")
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            _relativeDirectory = string.IsNullOrWhiteSpace(relativeDirectory) ? "gameplay" : relativeDirectory.Trim('/');
        }

        public static GameplaySaveFacade Shared { get; private set; } = CreateDefault();

        public string RelativeDirectory => _relativeDirectory;

        public bool Save(string slotId, out string error, string displayName = null)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                error = "slotId must not be empty.";
                return false;
            }

            if (!CanSaveNow(out error))
            {
                return false;
            }

            var normalizedSlotId = NormalizeSlotId(slotId);
            return _saveService.Save(
                GetRelativePath(normalizedSlotId),
                normalizedSlotId,
                string.IsNullOrWhiteSpace(displayName) ? normalizedSlotId : displayName,
                BuildSections(),
                out error);
        }

        public bool SaveAutoSlot(out string error)
        {
            return Save(AutoSaveSlotId, out error, AutoSaveDisplayName);
        }

        public bool Load(string slotId, out string error)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                error = "slotId must not be empty.";
                return false;
            }

            if (!CanLoadNow(out error))
            {
                return false;
            }

            var normalizedSlotId = NormalizeSlotId(slotId);
            if (!_saveService.TryPrepareLoad(
                    GetRelativePath(normalizedSlotId),
                    BuildSections(),
                    out var envelope,
                    out var preparedSections,
                    out error))
            {
                return false;
            }

            UIManager.Instance?.PrepareForGameplayLoad();

            if (!_saveService.ApplyPreparedLoad(preparedSections, out error))
            {
                return false;
            }

            EventBus.Publish(new GameEvents.GameStateLoaded
            {
                SlotId = envelope.slotId,
                Phase = TurnManager.Instance != null ? TurnManager.Instance.CurrentPhase : GamePhase.Prepare,
                TurnNumber = TurnManager.Instance != null ? TurnManager.Instance.CurrentTurn : 0,
                IsGameOver = TurnManager.Instance != null && TurnManager.Instance.IsGameOver
            });

            return true;
        }

        public IReadOnlyList<SaveSlotSummary> ListSlots()
        {
            return _saveService.ListSlots(_relativeDirectory);
        }

        public bool Delete(string slotId, out string error)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                error = "slotId must not be empty.";
                return false;
            }

            return _saveService.Delete(GetRelativePath(NormalizeSlotId(slotId)), out error);
        }

        public bool CanSaveNow(out string error)
        {
            error = null;

            if (MoneyManagerMissing() || BoardManagerMissing() || DeckManagerMissing() || TurnManagerMissing())
            {
                error = "Core gameplay systems are not initialized yet.";
                return false;
            }

            if (TurnManager.Instance.CurrentPhase != GamePhase.Action || TurnManager.Instance.ActionPhaseEnded)
            {
                error = "Gameplay saves are only allowed during the action phase safe point.";
                return false;
            }

            if (UICardDragController.Instance != null && UICardDragController.Instance.IsInteractionInProgress)
            {
                error = "Cannot save while a card drag interaction is active.";
                return false;
            }

            if (UIManager.Instance != null && UIManager.Instance.IsSettlementPlaybackBusy)
            {
                error = "Cannot save while settlement playback is active.";
                return false;
            }

            return true;
        }

        public bool CanLoadNow(out string error)
        {
            error = null;

            if (MoneyManagerMissing() || BoardManagerMissing() || DeckManagerMissing() || TurnManagerMissing())
            {
                error = "Core gameplay systems are not initialized yet.";
                return false;
            }

            if (UICardDragController.Instance != null && UICardDragController.Instance.IsInteractionInProgress)
            {
                error = "Cannot load while a card drag interaction is active.";
                return false;
            }

            if (UIManager.Instance != null && UIManager.Instance.IsSettlementPlaybackBusy)
            {
                error = "Cannot load while settlement playback is active.";
                return false;
            }

            return true;
        }

        private IReadOnlyList<ISaveSection> BuildSections()
        {
            return new ISaveSection[]
            {
                new EconomySaveSection(),
                new BoardSaveSection(),
                new DeckSaveSection(),
                new TurnSaveSection()
            };
        }

        private string GetRelativePath(string slotId)
        {
            return $"{_relativeDirectory}/{slotId}.json";
        }

        private static string NormalizeSlotId(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                throw new ArgumentException("slotId must not be empty.", nameof(slotId));
            }

            var fileName = slotId.Trim();
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            return fileName.Replace('/', '_').Replace('\\', '_');
        }

        private static bool MoneyManagerMissing() => BaoZuPo.Economy.MoneyManager.Instance == null;
        private static bool BoardManagerMissing() => BaoZuPo.Board.BoardManager.Instance == null;
        private static bool DeckManagerMissing() => BaoZuPo.Deck.DeckManager.Instance == null;
        private static bool TurnManagerMissing() => BaoZuPo.GameFlow.TurnManager.Instance == null;

        public static void SetSharedInstance(GameplaySaveFacade facade)
        {
            Shared = facade ?? CreateDefault();
        }

        private static GameplaySaveFacade CreateDefault()
        {
            return new GameplaySaveFacade(
                new SaveService(
                    new UnityJsonSaveSerializer(),
                    new PersistentDataPathSaveStorage(),
                    new SaveRuntimeOptions()),
                "gameplay");
        }
    }
}
