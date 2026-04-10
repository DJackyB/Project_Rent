using BaoZuPo.Card;
using BaoZuPo.GameFlow;
using Martian.Localization;

namespace BaoZuPo.UI
{
    /// <summary>
    /// Player-facing copy helper that now resolves through Martian localization first
    /// and falls back to the existing English strings when a table entry is missing.
    /// </summary>
    public static class GameText
    {
        private const string Table = "GameText";

        public static string Turn(int turn) => Resolve("GameText.Turn", $"Turn {turn}", turn);
        public static string Money(int money) => Resolve("GameText.Money", $"Money {money}", money);
        public static string Spent(int spent) => Resolve("GameText.Spent", $"Spent {spent}", spent);
        public static string PlayArea => Resolve("GameText.PlayArea", "Play Area");
        public static string Contracts => Resolve("GameText.Contracts", "Contracts");
        public static string Cost(int cost) => Resolve("GameText.Cost", $"Cost {cost}", cost);
        public static string FeedbackCost(int cost) => Resolve("GameText.FeedbackCost", $"Cost -{cost}", cost);
        public static string FeedbackLoan(int amount) => Resolve("GameText.FeedbackLoan", $"Loan -{amount}", amount);
        public static string InsufficientMoney => Resolve("GameText.InsufficientMoney", "Insufficient Money");
        public static string NextLoanDue(int turn) => Resolve("GameText.NextLoanDue", $"Next Payment Day: Turn {turn}", turn);
        public static string NextLoanAmount(int amount) => Resolve("GameText.NextLoanAmount", $"Next Mortgage: {amount}", amount);
        public static string LoanPreviewUnavailable => Resolve("GameText.LoanPreviewUnavailable", "Next Mortgage: --");
        public static string Wait(int turns) => Resolve("GameText.Wait", $"Wait {turns}", turns);
        public static string Lease => Resolve("GameText.Lease", "Lease");
        public static string Durability => Resolve("GameText.Durability", "Durability");
        public static string EmptyEquipmentSlot => Resolve("GameText.EmptyEquipmentSlot", "Empty Equipment Slot");
        public static string EmptyTenantSlot => Resolve("GameText.EmptyTenantSlot", "Empty Tenant Slot");
        public static string GameOverTitle => Resolve("GameText.GameOverTitle", "Game Over");
        public static string GameOverPlayAgain => Resolve("GameText.GameOverPlayAgain", "Play Again");
        public static string EndTurnButton => Resolve("GameText.EndTurnButton", "End Turn");
        public static string WaitingButton => Resolve("GameText.WaitingButton", "Waiting");
        public static string SettlementFallbackTitle => Resolve("GameText.SettlementFallbackTitle", "Settle");
        public static string SettlementBase => Resolve("GameText.SettlementBase", "Base");
        public static string SettlementBonus => Resolve("GameText.SettlementBonus", "Bonus");
        public static string SettlementMultiplier => Resolve("GameText.SettlementMultiplier", "Multiplier");
        public static string SettlementFinal => Resolve("GameText.SettlementFinal", "Final");
        public static string RewardTitle => Resolve("GameText.RewardTitle", "Choose a Reward");
        public static string RewardBoostedTitle => Resolve("GameText.RewardBoostedTitle", "Rare Reward!");
        public static string RewardSkip => Resolve("GameText.RewardSkip", "Skip");

        public static string RoomSummary(int roomNumber, int tenantCount, int tenantCapacity, int equipmentCount, int equipmentCapacity)
            => Resolve(
                "GameText.RoomSummary",
                $"Room {roomNumber}  Tenant {tenantCount}/{tenantCapacity}  Equip {equipmentCount}/{equipmentCapacity}",
                roomNumber,
                tenantCount,
                tenantCapacity,
                equipmentCount,
                equipmentCapacity);

        public static string GameOverInfo(int totalTurns, int finalMoney) => Resolve("GameText.GameOverInfo", $"You survived {totalTurns} turns\nTotal Money: {finalMoney}", totalTurns, finalMoney);

        public static string PhaseName(string phaseName)
        {
            return phaseName switch
            {
                "Prepare" => Resolve("GameText.PhaseName.Prepare", "Prepare"),
                "Action" => Resolve("GameText.PhaseName.Action", "Action"),
                "Settle" => Resolve("GameText.PhaseName.Settle", "Settle"),
                _ => phaseName
            };
        }

        public static string PhaseName(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.Prepare => Resolve("GameText.PhaseName.Prepare", "Prepare"),
                GamePhase.Action => Resolve("GameText.PhaseName.Action", "Action"),
                GamePhase.Settle => Resolve("GameText.PhaseName.Settle", "Settle"),
                _ => phase.ToString()
            };
        }

        public static string TypeLabel(CardType cardType)
        {
            return cardType switch
            {
                CardType.Tenant => Resolve("GameText.TypeLabel.Tenant", "Tenant"),
                CardType.Equipment => Resolve("GameText.TypeLabel.Equipment", "Equipment"),
                CardType.Event => Resolve("GameText.TypeLabel.Event", "Event"),
                CardType.Contract => Resolve("GameText.TypeLabel.Contract", "Contract"),
                _ => cardType.ToString()
            };
        }

        public static string SettlementRoomTitle(int roomNumber)
            => Resolve("GameText.SettlementRoomTitle", $"Room {roomNumber}", roomNumber);

        private static string Resolve(string entry, string fallback, params object[] arguments)
        {
            return LocalizationServices.Resolve(new LocalizationTextRef(Table, entry, fallback), arguments);
        }
    }
}
