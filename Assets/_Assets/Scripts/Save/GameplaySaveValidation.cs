using System.Collections.Generic;
using BaoZuPo.Card;

namespace BaoZuPo.Save
{
    internal static class GameplaySaveValidation
    {
        public static bool TryValidateCardState(CardRuntimeState state, out string error)
        {
            error = null;

            if (state == null)
            {
                error = "Encountered a null card state.";
                return false;
            }

            CardData data;
            try
            {
                data = CardDatabase.GetById(state.cardId);
            }
            catch (System.Exception exception)
            {
                error = $"Card database is unavailable while validating card '{state.cardId}': {exception.Message}";
                return false;
            }

            if (data == null)
            {
                error = $"Unknown cardId '{state.cardId}' in save data.";
                return false;
            }

            if (state.currentDurability < 0)
            {
                error = $"Card '{state.cardId}' has a negative durability value.";
                return false;
            }

            if (data.durability > 0 && state.currentDurability <= 0)
            {
                error = $"Card '{state.cardId}' must have positive durability while persisted.";
                return false;
            }

            if (state.currentWait < 0)
            {
                error = $"Card '{state.cardId}' has a negative wait value.";
                return false;
            }

            if (data.waitTurns > 0 && state.currentWait <= 0)
            {
                error = $"Card '{state.cardId}' must have positive wait while persisted.";
                return false;
            }

            return true;
        }

        public static bool TryValidateCardList(IReadOnlyList<CardRuntimeState> states, string label, out string error)
        {
            error = null;

            if (states == null)
            {
                error = $"{label} is missing.";
                return false;
            }

            for (int i = 0; i < states.Count; i++)
            {
                if (!TryValidateCardState(states[i], out error))
                {
                    error = $"{label}[{i}] invalid. {error}";
                    return false;
                }
            }

            return true;
        }
    }
}
