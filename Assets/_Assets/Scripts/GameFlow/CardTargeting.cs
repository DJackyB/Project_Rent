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

            if (card.cardType == CardType.Tenant || card.cardType == CardType.Equipment)
            {
                return CardPlayTargetKind.Room;
            }

            return card.targetKind;
        }

        public static CardPlayTargetKind InferTargetKindFromLegacyData(CardData card)
        {
            if (card == null)
            {
                return CardPlayTargetKind.PlayArea;
            }

            if (card.cardType == CardType.Tenant || card.cardType == CardType.Equipment)
            {
                return CardPlayTargetKind.Room;
            }

            return HasSelectedRoomEffect(card) ? CardPlayTargetKind.Room : CardPlayTargetKind.PlayArea;
        }

        public static bool TryValidateConfiguredTargetKind(CardData card, out string warning)
        {
            warning = null;
            if (card == null)
            {
                return true;
            }

            CardPlayTargetKind configuredTarget = card.targetKind;
            CardPlayTargetKind expectedTarget = card.cardType == CardType.Tenant || card.cardType == CardType.Equipment
                ? CardPlayTargetKind.Room
                : InferTargetKindFromLegacyData(card);

            if (configuredTarget == expectedTarget)
            {
                return true;
            }

            warning = $"Configured target kind '{configuredTarget}' does not match expected target kind '{expectedTarget}'.";
            return false;
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
