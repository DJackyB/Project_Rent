using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.GameFlow;
using Martian.Localization;

namespace BaoZuPo.Localization
{
    public readonly struct LocalizationSeedEntry
    {
        public LocalizationSeedEntry(string table, string key, string zhHans, string english)
        {
            Table = table;
            Key = key;
            ZhHans = zhHans;
            English = english;
        }

        public string Table { get; }
        public string Key { get; }
        public string ZhHans { get; }
        public string English { get; }
    }

    public static class GameText
    {
        public const string FontSeedCommonCharacters =
            "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            " +-*/=%().,:;!?[]{}<>#&@_\\|~" +
            "\uFF0C\u3002\uFF01\uFF1F\uFF1B\uFF1A\u3001\u201C\u201D\u2018\u2019\uFF08\uFF09\u3010\u3011\u300A\u300B\u2014\uFFE5\uFF1A";

        private static readonly LocalizationSeedEntry TurnLabel = Seed("ui.topbar.turn", "\u56DE\u5408 {0}", "Turn {0}");
        private static readonly LocalizationSeedEntry MoneyLabel = Seed("ui.topbar.money", "\u8D44\u91D1 {0}", "Money {0}");
        private static readonly LocalizationSeedEntry SpentLabel = Seed("ui.topbar.spent", "\u7D2F\u8BA1\u652F\u51FA {0}", "Spent {0}");
        private static readonly LocalizationSeedEntry PlayAreaLabel = Seed("ui.board.play_area", "\u51FA\u724C\u533A", "Play Area");
        private static readonly LocalizationSeedEntry ContractsLabel = Seed("ui.board.contracts", "\u5408\u540C\u533A", "Contracts");
        private static readonly LocalizationSeedEntry RoomSummaryLabel = Seed("ui.board.room_summary", "\u623F\u95F4 {0}  \u79DF\u5BA2 {1}/{2}  \u8BBE\u5907 {3}/{4}", "Room {0}  Tenant {1}/{2}  Equip {3}/{4}");
        private static readonly LocalizationSeedEntry CostLabel = Seed("ui.card.cost", "\u8D39\u7528 {0}", "Cost {0}");
        private static readonly LocalizationSeedEntry FeedbackCostLabel = Seed("ui.feedback.cost", "\u8D39\u7528 -{0}", "Cost -{0}");
        private static readonly LocalizationSeedEntry FeedbackLoanLabel = Seed("ui.feedback.loan", "\u8D37\u6B3E -{0}", "Loan -{0}");
        private static readonly LocalizationSeedEntry BaseRentLabel = Seed("ui.card.base_rent", "\u57FA\u7840\u79DF\u91D1 {0}", "Base Rent {0}");
        private static readonly LocalizationSeedEntry WaitLabel = Seed("ui.card.wait", "\u7B49\u5F85 {0}", "Wait {0}");
        private static readonly LocalizationSeedEntry LeaseLabel = Seed("ui.card.lease", "\u79DF\u671F", "Lease");
        private static readonly LocalizationSeedEntry DurabilityLabel = Seed("ui.card.durability", "\u8010\u4E45", "Durability");
        private static readonly LocalizationSeedEntry EmptyEquipmentLabel = Seed("ui.room.empty_equipment", "\u7A7A\u8BBE\u5907\u69FD", "Empty Equipment Slot");
        private static readonly LocalizationSeedEntry EmptyTenantLabel = Seed("ui.room.empty_tenant", "\u7A7A\u79DF\u5BA2\u69FD", "Empty Tenant Slot");
        private static readonly LocalizationSeedEntry GameOverTitleLabel = Seed("ui.game_over.title", "\u6E38\u620F\u7ED3\u675F", "Game Over");
        private static readonly LocalizationSeedEntry GameOverInfoLabel = Seed("ui.game_over.info", "\u4F60\u575A\u6301\u4E86 {0} \u56DE\u5408\n\u6700\u7EC8\u8D44\u91D1\uFF1A{1}", "You survived {0} turns\nFinal Money: {1}");
        private static readonly LocalizationSeedEntry EndTurnLabel = Seed("ui.phase.end_turn", "\u7ED3\u675F\u884C\u52A8", "End Turn");
        private static readonly LocalizationSeedEntry WaitingLabel = Seed("ui.phase.waiting", "\u7B49\u5F85\u4E2D", "Waiting");
        private static readonly LocalizationSeedEntry PrepareLabel = Seed("ui.phase.prepare", "\u51C6\u5907\u9636\u6BB5", "Prepare");
        private static readonly LocalizationSeedEntry ActionLabel = Seed("ui.phase.action", "\u884C\u52A8\u9636\u6BB5", "Action");
        private static readonly LocalizationSeedEntry SettleLabel = Seed("ui.phase.settle", "\u7ED3\u7B97\u9636\u6BB5", "Settle");
        private static readonly LocalizationSeedEntry TenantLabel = Seed("ui.card.type.tenant", "\u79DF\u5BA2", "Tenant");
        private static readonly LocalizationSeedEntry EquipmentLabel = Seed("ui.card.type.equipment", "\u8BBE\u5907", "Equipment");
        private static readonly LocalizationSeedEntry EventLabel = Seed("ui.card.type.event", "\u4E8B\u4EF6", "Event");
        private static readonly LocalizationSeedEntry ContractLabel = Seed("ui.card.type.contract", "\u5408\u540C", "Contract");
        private static readonly LocalizationSeedEntry SettlementRoomTitleLabel = Seed("ui.settlement.room_title", "\u623F\u95F4 {0}", "Room {0}");
        private static readonly LocalizationSeedEntry SettlementFallbackLabel = Seed("ui.settlement.fallback", "\u7ED3\u7B97", "Settle");
        private static readonly LocalizationSeedEntry SettlementBaseLabel = Seed("ui.settlement.base", "\u57FA\u7840", "Base");
        private static readonly LocalizationSeedEntry SettlementBonusLabel = Seed("ui.settlement.bonus", "\u52A0\u6210", "Bonus");
        private static readonly LocalizationSeedEntry SettlementMultiplierLabel = Seed("ui.settlement.multiplier", "\u500D\u7387", "Multiplier");
        private static readonly LocalizationSeedEntry SettlementFinalLabel = Seed("ui.settlement.final", "\u6700\u7EC8", "Final");

        private static readonly LocalizationSeedEntry[] Definitions =
        {
            TurnLabel,
            MoneyLabel,
            SpentLabel,
            PlayAreaLabel,
            ContractsLabel,
            RoomSummaryLabel,
            CostLabel,
            FeedbackCostLabel,
            FeedbackLoanLabel,
            BaseRentLabel,
            WaitLabel,
            LeaseLabel,
            DurabilityLabel,
            EmptyEquipmentLabel,
            EmptyTenantLabel,
            GameOverTitleLabel,
            GameOverInfoLabel,
            EndTurnLabel,
            WaitingLabel,
            PrepareLabel,
            ActionLabel,
            SettleLabel,
            TenantLabel,
            EquipmentLabel,
            EventLabel,
            ContractLabel,
            SettlementRoomTitleLabel,
            SettlementFallbackLabel,
            SettlementBaseLabel,
            SettlementBonusLabel,
            SettlementMultiplierLabel,
            SettlementFinalLabel
        };

        public static IReadOnlyList<LocalizationSeedEntry> GetDefinitions()
        {
            return Definitions;
        }

        public static string Turn(int turn) => Resolve(TurnLabel, turn);
        public static string Money(int money) => Resolve(MoneyLabel, money);
        public static string Spent(int spent) => Resolve(SpentLabel, spent);
        public static string PlayArea => Resolve(PlayAreaLabel);
        public static string Contracts => Resolve(ContractsLabel);
        public static string RoomSummary(int roomNumber, int tenantCount, int tenantCapacity, int equipmentCount, int equipmentCapacity)
            => Resolve(RoomSummaryLabel, roomNumber, tenantCount, tenantCapacity, equipmentCount, equipmentCapacity);
        public static string Cost(int cost) => Resolve(CostLabel, cost);
        public static string FeedbackCost(int cost) => Resolve(FeedbackCostLabel, cost);
        public static string FeedbackLoan(int amount) => Resolve(FeedbackLoanLabel, amount);
        public static string BaseRent(int amount) => Resolve(BaseRentLabel, amount);
        public static string Wait(int turns) => Resolve(WaitLabel, turns);
        public static string Lease => Resolve(LeaseLabel);
        public static string Durability => Resolve(DurabilityLabel);
        public static string EmptyEquipmentSlot => Resolve(EmptyEquipmentLabel);
        public static string EmptyTenantSlot => Resolve(EmptyTenantLabel);
        public static string GameOverTitle => Resolve(GameOverTitleLabel);
        public static string GameOverInfo(int totalTurns, int finalMoney) => Resolve(GameOverInfoLabel, totalTurns, finalMoney);
        public static string EndTurnButton => Resolve(EndTurnLabel);
        public static string WaitingButton => Resolve(WaitingLabel);
        public static string SettlementFallbackTitle => Resolve(SettlementFallbackLabel);
        public static string SettlementBase => Resolve(SettlementBaseLabel);
        public static string SettlementBonus => Resolve(SettlementBonusLabel);
        public static string SettlementMultiplier => Resolve(SettlementMultiplierLabel);
        public static string SettlementFinal => Resolve(SettlementFinalLabel);
        public static string SettlementRoomTitle(int roomNumber) => Resolve(SettlementRoomTitleLabel, roomNumber);

        public static string PhaseName(string phaseName)
        {
            return phaseName switch
            {
                "Prepare" => Resolve(PrepareLabel),
                "Action" => Resolve(ActionLabel),
                "Settle" => Resolve(SettleLabel),
                _ => phaseName
            };
        }

        public static string PhaseName(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.Prepare => Resolve(PrepareLabel),
                GamePhase.Action => Resolve(ActionLabel),
                GamePhase.Settle => Resolve(SettleLabel),
                _ => phase.ToString()
            };
        }

        public static string TypeLabel(CardType cardType)
        {
            return cardType switch
            {
                CardType.Tenant => Resolve(TenantLabel),
                CardType.Equipment => Resolve(EquipmentLabel),
                CardType.Event => Resolve(EventLabel),
                CardType.Contract => Resolve(ContractLabel),
                _ => cardType.ToString()
            };
        }

        public static IEnumerable<string> GetFontSeedTexts()
        {
            yield return FontSeedCommonCharacters;

            foreach (var definition in Definitions)
            {
                if (!string.IsNullOrWhiteSpace(definition.ZhHans))
                {
                    yield return definition.ZhHans;
                }

                if (!string.IsNullOrWhiteSpace(definition.English))
                {
                    yield return definition.English;
                }
            }

            yield return Turn(99);
            yield return Money(99999);
            yield return Spent(99999);
            yield return RoomSummary(9, 9, 9, 9, 9);
            yield return Cost(999);
            yield return BaseRent(999);
            yield return Wait(9);
            yield return GameOverInfo(99, 99999);
        }

        private static LocalizationSeedEntry Seed(string key, string zhHans, string english)
        {
            return new LocalizationSeedEntry(BaoZuPoLocalizationConstants.UiTable, key, zhHans, english);
        }

        private static string Resolve(LocalizationSeedEntry seed, params object[] arguments)
        {
            return LocalizationServices.Resolve(new LocalizationTextRef(seed.Table, seed.Key, seed.ZhHans), arguments);
        }
    }
}
