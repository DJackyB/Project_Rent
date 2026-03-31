using BaoZuPo.Card;
using BaoZuPo.GameFlow;

namespace BaoZuPo.UI
{
    public static class GameText
    {
        public static string Turn(int turn) => $"Turn {turn}";
        public static string Money(int money) => $"Money {money}";
        public static string Spent(int spent) => $"Spent {spent}";
        public static string PlayArea => "Play Area";
        public static string Contracts => "Contracts";
        public static string Cost(int cost) => $"Cost {cost}";
        public static string FeedbackCost(int cost) => $"Cost -{cost}";
        public static string FeedbackLoan(int amount) => $"Loan -{amount}";
        public static string Wait(int turns) => $"Wait {turns}";
        public static string Lease => "Lease";
        public static string Durability => "Durability";
        public static string EmptyEquipmentSlot => "Empty Equipment Slot";
        public static string EmptyTenantSlot => "Empty Tenant Slot";
        public static string GameOverTitle => "Game Over";
        public static string EndTurnButton => "End Turn";
        public static string WaitingButton => "Waiting";
        public static string SettlementFallbackTitle => "Settle";
        public static string SettlementBase => "Base";
        public static string SettlementBonus => "Bonus";
        public static string SettlementMultiplier => "Multiplier";
        public static string SettlementFinal => "Final";
        public static string RewardTitle => "Choose a Reward";
        public static string RewardBoostedTitle => "Rare Reward!";
        public static string RewardSkip => "Skip";

        public static string RoomSummary(int roomNumber, int tenantCount, int tenantCapacity, int equipmentCount, int equipmentCapacity)
            => $"Room {roomNumber}  Tenant {tenantCount}/{tenantCapacity}  Equip {equipmentCount}/{equipmentCapacity}";

        public static string GameOverInfo(int totalTurns, int finalMoney)
            => $"You survived {totalTurns} turns\nFinal Money: {finalMoney}";

        public static string PhaseName(string phaseName)
        {
            return phaseName switch
            {
                "Prepare" => "Prepare",
                "Action" => "Action",
                "Settle" => "Settle",
                _ => phaseName
            };
        }

        public static string PhaseName(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.Prepare => "Prepare",
                GamePhase.Action => "Action",
                GamePhase.Settle => "Settle",
                _ => phase.ToString()
            };
        }

        public static string TypeLabel(CardType cardType)
        {
            return cardType switch
            {
                CardType.Tenant => "Tenant",
                CardType.Equipment => "Equipment",
                CardType.Event => "Event",
                CardType.Contract => "Contract",
                _ => cardType.ToString()
            };
        }

        public static string SettlementRoomTitle(int roomNumber) => $"Room {roomNumber}";
    }
}
