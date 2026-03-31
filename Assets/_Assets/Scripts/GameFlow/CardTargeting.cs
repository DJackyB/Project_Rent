using System;
using BaoZuPo.Card;

namespace BaoZuPo.GameFlow
{
    public static class CardTargeting
    {
        public static CardPlayTargetKind GetRequiredTargetKind(CardData card)
        {
            if (card == null)
            {
                return CardPlayTargetKind.PlayArea;
            }

            return card.targetKind;
        }

        public static bool PersistsInRoom(CardData card)
        {
            if (card == null)
            {
                return false;
            }

            return GetRequiredTargetKind(card) == CardPlayTargetKind.Room
                && (card.cardType == CardType.Tenant || card.cardType == CardType.Equipment);
        }

        public static bool PersistsAsContract(CardData card)
        {
            if (card == null)
            {
                return false;
            }

            return GetRequiredTargetKind(card) == CardPlayTargetKind.PlayArea
                && card.cardType == CardType.Contract;
        }

        public static bool TryValidateConfiguredTargetKind(CardData card, out string warning)
        {
            warning = null;
            if (card == null)
            {
                return true;
            }

            if (!Enum.IsDefined(typeof(CardPlayTargetKind), card.targetKind))
            {
                warning = $"Configured target kind '{(int)card.targetKind}' is invalid.";
                return false;
            }

            if (HasSelectedRoomEffect(card) && card.targetKind != CardPlayTargetKind.Room)
            {
                warning = "Cards that use SelectedRoom effects must use targetKind Room.";
                return false;
            }

            return true;
        }

        private static bool HasSelectedRoomEffect(CardData card)
        {
            return ContainsSelectedRoom(card.preEffect)
                || ContainsSelectedRoom(card.instantEffect)
                || ContainsSelectedRoom(card.settleEffect)
                || ContainsSelectedRoom(card.destroyEffect);
        }

        private static bool ContainsSelectedRoom(string effectString)
        {
            return !string.IsNullOrWhiteSpace(effectString) && effectString.Contains("SelectedRoom");
        }
    }
}
