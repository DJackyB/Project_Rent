using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.GameFlow;

namespace BaoZuPo.UI
{
    public static class UIStrings
    {
        public const string FontSeedCommonCharacters =
            "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            " +-*/=%().,:;!?[]{}<>#&@_\\|~" +
            "，。！？；：、“”‘’（）【】《》〈〉—…·￥";

        public static string Turn(int turn) => LocalizationManager.UseChinese ? $"回合 {turn}" : $"Turn {turn}";
        public static string Money(int money) => LocalizationManager.UseChinese ? $"资金 {money}" : $"Money {money}";
        public static string Spent(int spent) => LocalizationManager.UseChinese ? $"累计支出 {spent}" : $"Spent {spent}";
        public static string PlayArea => LocalizationManager.UseChinese ? "出牌区" : "Play Area";
        public static string Contracts => LocalizationManager.UseChinese ? "合同区" : "Contracts";
        public static string RoomSummary(int roomNumber, int tenantCount, int tenantCapacity, int equipmentCount, int equipmentCapacity)
            => LocalizationManager.UseChinese
                ? $"房间 {roomNumber}  租客 {tenantCount}/{tenantCapacity}  设备 {equipmentCount}/{equipmentCapacity}"
                : $"Room {roomNumber}  Tenant {tenantCount}/{tenantCapacity}  Equip {equipmentCount}/{equipmentCapacity}";

        public static string Cost(int cost) => LocalizationManager.UseChinese ? $"费用 {cost}" : $"Cost {cost}";
        public static string FeedbackCost(int cost) => LocalizationManager.UseChinese ? $"费用 -{cost}" : $"Cost -{cost}";
        public static string FeedbackLoan(int amount) => LocalizationManager.UseChinese ? $"贷款 -{amount}" : $"Loan -{amount}";
        public static string BaseRent(int amount) => LocalizationManager.UseChinese ? $"基础租金 {amount}" : $"Base Rent {amount}";
        public static string Wait(int turns) => LocalizationManager.UseChinese ? $"等待 {turns}" : $"Wait {turns}";
        public static string Lease => LocalizationManager.UseChinese ? "租期" : "Lease";
        public static string Durability => LocalizationManager.UseChinese ? "耐久" : "Durability";
        public static string EmptyEquipmentSlot => LocalizationManager.UseChinese ? "空设备槽" : "Empty Equipment Slot";
        public static string EmptyTenantSlot => LocalizationManager.UseChinese ? "空租客槽" : "Empty Tenant Slot";
        public static string GameOverTitle => LocalizationManager.UseChinese ? "游戏结束" : "Game Over";
        public static string GameOverInfo(int totalTurns, int finalMoney)
            => LocalizationManager.UseChinese
                ? $"你坚持了 {totalTurns} 回合\n最终资金：{finalMoney}"
                : $"You survived {totalTurns} turns\nFinal Money: {finalMoney}";

        public static string EndTurnButton => LocalizationManager.UseChinese ? "结束行动" : "End Turn";
        public static string WaitingButton => LocalizationManager.UseChinese ? "等待中" : "Waiting";

        public static string PhaseName(string phaseName)
        {
            return phaseName switch
            {
                "Prepare" => LocalizationManager.UseChinese ? "准备阶段" : "Prepare",
                "Action" => LocalizationManager.UseChinese ? "行动阶段" : "Action",
                "Settle" => LocalizationManager.UseChinese ? "结算阶段" : "Settle",
                _ => phaseName
            };
        }

        public static string PhaseName(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.Prepare => LocalizationManager.UseChinese ? "准备阶段" : "Prepare",
                GamePhase.Action => LocalizationManager.UseChinese ? "行动阶段" : "Action",
                GamePhase.Settle => LocalizationManager.UseChinese ? "结算阶段" : "Settle",
                _ => phase.ToString()
            };
        }

        public static string TypeLabel(CardType cardType)
        {
            return cardType switch
            {
                CardType.Tenant => LocalizationManager.UseChinese ? "租客" : "Tenant",
                CardType.Equipment => LocalizationManager.UseChinese ? "设备" : "Equipment",
                CardType.Event => LocalizationManager.UseChinese ? "事件" : "Event",
                CardType.Contract => LocalizationManager.UseChinese ? "合同" : "Contract",
                _ => cardType.ToString()
            };
        }

        public static string SettlementRoomTitle(int roomNumber)
            => LocalizationManager.UseChinese ? $"房间 {roomNumber}" : $"Room {roomNumber}";

        public static string SettlementFallbackTitle => LocalizationManager.UseChinese ? "结算" : "Settle";
        public static string SettlementBase => LocalizationManager.UseChinese ? "基础" : "Base";
        public static string SettlementBonus => LocalizationManager.UseChinese ? "加成" : "Bonus";
        public static string SettlementMultiplier => LocalizationManager.UseChinese ? "倍率" : "Multiplier";
        public static string SettlementFinal => LocalizationManager.UseChinese ? "最终" : "Final";

        public static string RewardTitle => LocalizationManager.UseChinese ? "选择奖励" : "Choose a Reward";
        public static string RewardBoostedTitle => LocalizationManager.UseChinese ? "稀有奖励!" : "Rare Reward!";
        public static string RewardSkip => LocalizationManager.UseChinese ? "跳过" : "Skip";

        public static IEnumerable<string> GetFontSeedTexts()
        {
            yield return FontSeedCommonCharacters;
            yield return "回合 99";
            yield return "资金 99999";
            yield return "累计支出 99999";
            yield return "出牌区";
            yield return "合同区";
            yield return "房间 9  租客 9/9  设备 9/9";
            yield return "费用 999";
            yield return "基础租金 999";
            yield return "等待 9";
            yield return "租期 9";
            yield return "耐久 9";
            yield return "空租客槽";
            yield return "空设备槽";
            yield return "游戏结束";
            yield return "你坚持了 99 回合\n最终资金：99999";
            yield return "结束行动";
            yield return "等待中";
            yield return "准备阶段";
            yield return "行动阶段";
            yield return "结算阶段";
            yield return "租客";
            yield return "设备";
            yield return "事件";
            yield return "合同";
            yield return "基础 +100";
            yield return "加成 +100";
            yield return "倍率 x2";
            yield return "最终 +200";
            yield return "贷款 -999";
            yield return "Turn 99";
            yield return "Money 99999";
            yield return "Spent 99999";
            yield return "Play Area";
            yield return "Contracts";
            yield return "Room 9  Tenant 9/9  Equip 9/9";
            yield return "Cost 999";
            yield return "Cost -999";
            yield return "Base Rent 999";
            yield return "Loan -999";
            yield return "Wait 9";
            yield return "Lease 9";
            yield return "Durability 9";
            yield return "Empty Tenant Slot";
            yield return "Empty Equipment Slot";
            yield return "Game Over";
            yield return "You survived 99 turns\nFinal Money: 99999";
            yield return "End Turn";
            yield return "Waiting";
            yield return "Prepare";
            yield return "Action";
            yield return "Settle";
            yield return "Tenant";
            yield return "Equipment";
            yield return "Event";
            yield return "Contract";
            yield return "Base +100";
            yield return "Bonus +100";
            yield return "Multiplier x2";
            yield return "Final +200";
            yield return "选择奖励";
            yield return "稀有奖励!";
            yield return "跳过";
            yield return "Choose a Reward";
            yield return "Rare Reward!";
            yield return "Skip";
        }
    }
}
