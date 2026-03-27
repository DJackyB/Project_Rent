using System;
using BaoZuPo.Board;
using BaoZuPo.Deck;
using BaoZuPo.Economy;
using BaoZuPo.GameFlow;
using BaoZuPo.UI;
using Martian.Save.Runtime;

namespace BaoZuPo.Save
{
    internal sealed class EconomySaveSection : SaveSectionBase<EconomySaveState>
    {
        public override string Key => "baozupo.economy";

        protected override EconomySaveState CaptureTypedState()
        {
            return MoneyManager.Instance.CaptureState();
        }

        protected override bool TryValidateTypedState(EconomySaveState state, out string error)
        {
            error = null;

            if (state == null)
            {
                error = "Economy section is missing.";
                return false;
            }

            if (state.currentMoney < 0)
            {
                error = "Economy section has a negative currentMoney.";
                return false;
            }

            if (state.totalSpent < 0)
            {
                error = "Economy section has a negative totalSpent.";
                return false;
            }

            return true;
        }

        protected override void ApplyTypedState(EconomySaveState state)
        {
            MoneyManager.Instance.RestoreState(state);
        }
    }

    internal sealed class BoardSaveSection : SaveSectionBase<BoardSaveState>
    {
        public override string Key => "baozupo.board";

        protected override BoardSaveState CaptureTypedState()
        {
            return BoardManager.Instance.CaptureState();
        }

        protected override bool TryValidateTypedState(BoardSaveState state, out string error)
        {
            error = null;

            if (state == null)
            {
                error = "Board section is missing.";
                return false;
            }

            if (state.rooms == null)
            {
                error = "Board section is missing rooms.";
                return false;
            }

            for (int i = 0; i < state.rooms.Count; i++)
            {
                var room = state.rooms[i];
                if (room == null)
                {
                    error = $"Board room {i} is null.";
                    return false;
                }

                if (room.roomIndex != i)
                {
                    error = $"Board room index mismatch at {i}.";
                    return false;
                }

                if (room.tenantSlotCapacity < 0 || room.equipmentSlotCapacity < 0)
                {
                    error = $"Board room {i} has a negative slot capacity.";
                    return false;
                }

                if (room.tenants == null)
                {
                    error = $"Board room {i} tenants are missing.";
                    return false;
                }

                if (room.equipments == null)
                {
                    error = $"Board room {i} equipments are missing.";
                    return false;
                }

                if (room.tenants.Count > room.tenantSlotCapacity)
                {
                    error = $"Board room {i} exceeds tenant capacity.";
                    return false;
                }

                if (room.equipments.Count > room.equipmentSlotCapacity)
                {
                    error = $"Board room {i} exceeds equipment capacity.";
                    return false;
                }

                if (!GameplaySaveValidation.TryValidateCardList(room.tenants, $"Board room {i} tenants", out error))
                {
                    return false;
                }

                if (!GameplaySaveValidation.TryValidateCardList(room.equipments, $"Board room {i} equipments", out error))
                {
                    return false;
                }
            }

            return GameplaySaveValidation.TryValidateCardList(state.contracts, "Board contracts", out error);
        }

        protected override void ApplyTypedState(BoardSaveState state)
        {
            BoardManager.Instance.RestoreState(state);
        }
    }

    internal sealed class DeckSaveSection : SaveSectionBase<DeckSaveState>
    {
        public override string Key => "baozupo.deck";

        protected override DeckSaveState CaptureTypedState()
        {
            return DeckManager.Instance.CaptureState();
        }

        protected override bool TryValidateTypedState(DeckSaveState state, out string error)
        {
            error = null;

            if (state == null)
            {
                error = "Deck section is missing.";
                return false;
            }

            if (state.maxHandSize < 1)
            {
                error = "Deck section has an invalid maxHandSize.";
                return false;
            }

            if (!GameplaySaveValidation.TryValidateCardList(state.drawPile, "Deck drawPile", out error))
            {
                return false;
            }

            if (!GameplaySaveValidation.TryValidateCardList(state.hand, "Deck hand", out error))
            {
                return false;
            }

            if (!GameplaySaveValidation.TryValidateCardList(state.discardPile, "Deck discardPile", out error))
            {
                return false;
            }

            if (state.hand.Count > state.maxHandSize)
            {
                error = "Deck hand exceeds maxHandSize.";
                return false;
            }

            return true;
        }

        protected override void ApplyTypedState(DeckSaveState state)
        {
            DeckManager.Instance.RestoreState(state);
        }
    }

    internal sealed class TurnSaveSection : SaveSectionBase<TurnSaveState>
    {
        public override string Key => "baozupo.turn";

        protected override TurnSaveState CaptureTypedState()
        {
            return TurnManager.Instance.CaptureState();
        }

        protected override bool TryValidateTypedState(TurnSaveState state, out string error)
        {
            error = null;

            if (state == null)
            {
                error = "Turn section is missing.";
                return false;
            }

            if (state.currentTurn < 0)
            {
                error = "Turn section has a negative currentTurn.";
                return false;
            }

            if (state.loanPaymentCount < 0)
            {
                error = "Turn section has a negative loanPaymentCount.";
                return false;
            }

            if (!Enum.IsDefined(typeof(GamePhase), state.currentPhase))
            {
                error = $"Turn section has an invalid phase '{state.currentPhase}'.";
                return false;
            }

            return true;
        }

        protected override void ApplyTypedState(TurnSaveState state)
        {
            TurnManager.Instance.RestoreState(state);
        }
    }

    internal sealed class SettingsSaveSection : SaveSectionBase<SettingsSaveState>
    {
        public override string Key => "baozupo.settings";

        protected override SettingsSaveState CaptureTypedState()
        {
            return LocalizationManager.CaptureSettingsState();
        }

        protected override bool TryValidateTypedState(SettingsSaveState state, out string error)
        {
            error = null;

            if (state == null)
            {
                error = "Settings section is missing.";
                return false;
            }

            if (!Enum.IsDefined(typeof(AppLanguage), state.language))
            {
                error = $"Settings section has an invalid language '{state.language}'.";
                return false;
            }

            return true;
        }

        protected override void ApplyTypedState(SettingsSaveState state)
        {
            LocalizationManager.ApplySettingsState(state);
        }
    }
}
