using System;
using System.Collections.Generic;
using BaoZuPo.Card;

namespace BaoZuPo.GameFlow
{
    public static class CardTargeting
    {
        private static readonly HashSet<string> ExplicitRoomTargetEffects = new(StringComparer.Ordinal)
        {
            "AddMoneyBySelectedRoomTenantCount",
            "AddTenantDurabilityInSelectedRoom",
            "EvictTenantInSelectedRoom",
            "ExpandSlot",
            "MoveTenantToEmptyRoom",
            "SpawnRandomTenantInSelectedRoom",
            "TriggerSelectedRoomSettle",
        };

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
                warning = "Cards that use room-dependent effects must use targetKind Room.";
                return false;
            }

            return true;
        }

        private static bool HasSelectedRoomEffect(CardData card)
        {
            return ContainsRoomDependentEffect(card.preEffect)
                || ContainsRoomDependentEffect(card.instantEffect)
                || ContainsRoomDependentEffect(card.settleEffect)
                || ContainsRoomDependentEffect(card.destroyEffect);
        }

        private static bool ContainsRoomDependentEffect(string effectString)
        {
            if (string.IsNullOrWhiteSpace(effectString))
            {
                return false;
            }

            var segments = effectString.Split('|');
            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i].Trim();
                if (string.IsNullOrWhiteSpace(segment))
                {
                    continue;
                }

                int separatorIndex = segment.IndexOf(';');
                string effectId = separatorIndex >= 0 ? segment[..separatorIndex] : segment;
                if (ExplicitRoomTargetEffects.Contains(effectId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
