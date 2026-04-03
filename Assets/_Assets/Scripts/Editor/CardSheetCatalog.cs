using System.Linq;
using BaoZuPo.GameFlow;

namespace BaoZuPo.Editor
{
    /// <summary>
    /// 卡牌数据目录（代码侧定义）。
    /// 在代码中定义所有卡牌和卡牌库，既可以直接用于编辑器工具，也可以序列化到 Excel。
    ///
    /// 职责：
    /// - CardRow 数组：定义所有卡牌的数值、效果、标签
    /// - LibraryRow 数组：定义各个卡牌库及其卡牌列表
    /// - 导表工具从此读取数据，生成 CardData.asset 和 CardLibrary.asset
    /// - 当前主要用于维护牌库定义与固定样例卡数据
    /// </summary>
    internal static class CardSheetCatalog
    {
        /// <summary>
        /// 卡牌行定义。对应 Excel 中的一行卡牌数据。
        /// </summary>
        internal sealed class CardRow
        {
            public int CardId { get; set; }             // 卡牌唯一 ID
            public string CardName { get; set; }         // 卡牌中文名
            public string Description { get; set; }      // 卡牌描述
            public string CardType { get; set; }         // 卡牌类型（Card_Tenant、Card_Equipt、Card_Event、Card_Contract）
            public int Rarity { get; set; }              // 稀有度（0-3 或 0-4）
            public string CardArt { get; set; }          // 美术资源路径
            public int Cost { get; set; }                // 打出成本（金钱）
            public int BaseRent { get; set; }            // 基础租金/收入
            public string TargetKind { get; set; }       // 目标类型（Room、PlayArea 等）
            public int WaitTurns { get; set; }           // 等待回合（放置后多少回合才生效）
            public int Durability { get; set; }          // 耐久度（0=无限）
            public string PreEffect { get; set; }        // 放置时触发的效果
            public string InstantEffect { get; set; }    // 打出时立即触发的效果
            public string SettleEffect { get; set; }     // 结算时触发的效果
            public string DestroyEffect { get; set; }    // 销毁时触发的效果
            public string Tag { get; set; }              // 标签（用于分类和策略识别）
        }

        /// <summary>
        /// 卡牌库行定义。定义一个卡牌库及其包含的卡牌列表。
        /// </summary>
        internal sealed class LibraryRow
        {
            public string AssetName { get; set; }        // 资产文件名（不含扩展名）
            public string LibraryId { get; set; }         // 库的唯一标识
            public string DisplayName { get; set; }       // 库的显示名（UI 中使用）
            public int[] CardIds { get; set; }            // 库中包含的卡牌 ID 数组
        }

        /// <summary>
        /// 所有卡牌的数据定义。
        /// 每一行对应一张卡牌，包含其完整的数值、效果、标签。
        /// </summary>
        internal static readonly CardRow[] Rows =
        {
            R(1001, "Worker", "Simple worker, pays on time.", "Card_Tenant", 0, 50, 30, nameof(CardPlayTargetKind.Room), 0, 5, "", "AddMoney;60", "", "ReduceMoney;30", "SteadyRent|Core"),
            R(1002, "White Collar", "Stable income, clean room.", "Card_Tenant", 1, 120, 60, nameof(CardPlayTargetKind.Room), 0, 8, "", "AddMoney;120", "", "ReduceMoney;60", "SteadyRent|Core"),
            R(1003, "Programmer", "High pay, often overtimes.", "Card_Tenant", 2, 200, 100, nameof(CardPlayTargetKind.Room), 0, 4, "", "AddMoney;200", "", "ReduceMoney;100", "QuickTurnover|Payoff"),
            R(1004, "Senior Teacher", "Quiet old tenant, never leaves.", "Card_Tenant", 1, 80, 25, nameof(CardPlayTargetKind.Room), 0, 0, "", "AddMoney;50", "", "ReduceMoney;25", "SteadyRent|Anchor"),
            R(1005, "Student", "Cheap but short lease.", "Card_Tenant", 0, 30, 20, nameof(CardPlayTargetKind.Room), 0, 4, "", "AddMoney;40", "", "ReduceMoney;20", "VacancyEconomy|Starter"),
            R(1006, "Streamer", "Lavish pay but moves often.", "Card_Tenant", 2, 150, 120, nameof(CardPlayTargetKind.Room), 0, 3, "", "AddMoney;240", "", "ReduceMoney;120", "QuickTurnover|Payoff"),
            R(1007, "Doctor", "High deposit, long lease.", "Card_Tenant", 2, 250, 80, nameof(CardPlayTargetKind.Room), 0, 10, "", "AddMoney;160", "", "ReduceMoney;80", "SteadyRent|Core"),
            R(1008, "Chef", "Sturdy and reliable.", "Card_Tenant", 1, 100, 50, nameof(CardPlayTargetKind.Room), 0, 6, "", "AddMoney;100", "", "ReduceMoney;50", "EquipmentStack|Support"),
            R(1009, "Painter", "Needs time to settle in.", "Card_Tenant", 1, 60, 40, nameof(CardPlayTargetKind.Room), 2, 6, "", "AddMoney;80", "", "ReduceMoney;40", "Expansion|Setup"),
            R(1010, "Courier", "Comes fast, goes fast.", "Card_Tenant", 0, 40, 35, nameof(CardPlayTargetKind.Room), 0, 3, "", "AddMoney;70", "", "ReduceMoney;35", "QuickTurnover|Bridge"),
            R(2001, "Air Conditioner", "Improves comfort.", "Card_Equipt", 1, 80, 0, nameof(CardPlayTargetKind.Room), 0, 5, "", "", "AddMoney;10", "", "SteadyRent|Support"),
            R(2002, "Bunk Bed", "One more tenant.", "Card_Equipt", 2, 100, 0, nameof(CardPlayTargetKind.Room), 0, 0, "", "ExpandSlot;1", "", "", "Expansion|Engine"),
            R(2003, "Water Heater", "Basic facility, durable.", "Card_Equipt", 0, 60, 0, nameof(CardPlayTargetKind.Room), 0, 8, "", "", "AddMoney;5", "", "SteadyRent|Support"),
            R(2004, "WiFi Router", "Life necessity.", "Card_Equipt", 1, 90, 0, nameof(CardPlayTargetKind.Room), 0, 6, "", "", "AddMoney;15", "", "QuickTurnover|Support"),
            R(2005, "Washer", "Laundry convenience.", "Card_Equipt", 1, 70, 0, nameof(CardPlayTargetKind.Room), 0, 7, "", "", "AddMoney;8", "", "EquipmentStack|Support"),
            R(3001, "Rent Bonus", "Extra rent income.", "Card_Event", 0, 0, 0, nameof(CardPlayTargetKind.PlayArea), 0, 0, "", "AddMoney;200", "", "", "BurstSettlement|Payoff"),
            R(3002, "Agent Refer", "Pay for 2 more cards.", "Card_Event", 1, 50, 0, nameof(CardPlayTargetKind.PlayArea), 0, 0, "", "DrawCard;2", "", "", "QuickTurnover|Bridge"),
            R(3003, "Renewal", "Persuade all to stay.", "Card_Event", 1, 80, 0, nameof(CardPlayTargetKind.PlayArea), 0, 0, "", "AddTenantDurability;3", "", "", "SteadyRent|Support"),
            R(3004, "Maintenance", "Repair all equipment.", "Card_Event", 1, 60, 0, nameof(CardPlayTargetKind.PlayArea), 0, 0, "", "AddEquipmentDurability;3", "", "", "EquipmentStack|Support"),
            R(3005, "Urgent Loan", "Bank emergency cash.", "Card_Event", 2, 0, 0, nameof(CardPlayTargetKind.PlayArea), 0, 0, "", "AddMoney;500", "", "", "BurstSettlement|Payoff"),
            R(1011, "Union Clerk", "Reliable office income with minimal churn.", "Card_Tenant", 1, 70, 35, nameof(CardPlayTargetKind.Room), 0, 6, "", "", "", "ReduceMoney;20", "SteadyRent|Core"),
            R(1012, "Night Nurse", "Long shifts, stable return, hard to shake.", "Card_Tenant", 2, 95, 45, nameof(CardPlayTargetKind.Room), 0, 7, "", "", "", "ReduceMoney;30", "SteadyRent|Core"),
            R(2006, "Lease Ledger", "Turns each room into a cleaner revenue line.", "Card_Equipt", 1, 85, 0, nameof(CardPlayTargetKind.Room), 0, 5, "", "", "AddMoney;15", "", "SteadyRent|Support"),
            R(3006, "Rent Reminder", "A small push that keeps tenants around and the cash flowing.", "Card_Event", 0, 40, 0, nameof(CardPlayTargetKind.PlayArea), 0, 0, "", "AddMoney;60|AddTenantDurability;2", "", "", "SteadyRent|Tempo"),
            R(4001, "Long Lease", "Locks in a low-risk income lane for several turns.", "Card_Contract", 1, 120, 0, nameof(CardPlayTargetKind.PlayArea), 0, 4, "", "", "AddMoney;20|AddTenantDurability;1", "", "SteadyRent|Anchor"),
            R(1013, "Temp Worker", "Cheap entry, quick payoff, quick exit.", "Card_Tenant", 0, 55, 25, nameof(CardPlayTargetKind.Room), 0, 4, "", "", "", "AddMoney;50", "QuickTurnover|Core"),
            R(1014, "Night Rider", "Short stay, high pace, and easy to cash out.", "Card_Tenant", 1, 80, 35, nameof(CardPlayTargetKind.Room), 0, 3, "", "", "", "AddMoney;70", "QuickTurnover|Payoff"),
            R(2007, "Clearance Sign", "Keeps a busy room converting motion into cash.", "Card_Equipt", 1, 65, 0, nameof(CardPlayTargetKind.Room), 0, 4, "", "", "AddMoney;10", "", "QuickTurnover|Support"),
            R(3007, "Recruiting Call", "Immediate draw to refill a fast hand.", "Card_Event", 1, 40, 0, nameof(CardPlayTargetKind.PlayArea), 0, 0, "", "DrawCard;2", "", "", "QuickTurnover|Tempo"),
            R(3008, "Relocation Order", "Moves the lead tenant out and frees the room for the next plan.", "Card_Event", 1, 25, 0, nameof(CardPlayTargetKind.Room), 0, 0, "", "MoveTenantToEmptyRoom|AddMoney;20", "", "", "QuickTurnover|Control"),
            R(1015, "Handyman", "Keeps the room's hardware alive while paying normal rent.", "Card_Tenant", 1, 60, 30, nameof(CardPlayTargetKind.Room), 0, 5, "", "", "AddEquipmentDurability;1", "", "EquipmentStack|Core"),
            R(2008, "Extra Shelf", "Adds one more tenant slot to a built-up room.", "Card_Equipt", 1, 80, 0, nameof(CardPlayTargetKind.Room), 0, 5, "", "ExpandSlot;1", "", "", "EquipmentStack|Engine"),
            R(2009, "Power Strip", "A modest upgrade that keeps stacked devices productive.", "Card_Equipt", 1, 70, 0, nameof(CardPlayTargetKind.Room), 0, 5, "", "", "AddMoney;10|AddEquipmentDurability;1", "", "EquipmentStack|Support"),
            R(2010, "Laundry Stack", "Improves room comfort and keeps the tenant rolling longer.", "Card_Equipt", 2, 95, 0, nameof(CardPlayTargetKind.Room), 0, 6, "", "", "AddMoney;12|AddTenantDurabilityInSelectedRoom;1", "", "EquipmentStack|Sustain"),
            R(3009, "Maintenance Crew", "Repairs the building and turns upkeep into a small payout.", "Card_Event", 1, 55, 0, nameof(CardPlayTargetKind.PlayArea), 0, 0, "", "AddEquipmentDurability;3|AddMoney;40", "", "", "EquipmentStack|Bridge"),
            R(1016, "Subletter", "Low rent that converts open units into side income.", "Card_Tenant", 0, 45, 20, nameof(CardPlayTargetKind.Room), 0, 4, "", "", "AddMoneyByEmptyRooms;10", "", "VacancyEconomy|Core"),
            R(2011, "Open House Sign", "The emptier the building, the better this performs.", "Card_Equipt", 1, 70, 0, nameof(CardPlayTargetKind.Room), 0, 4, "", "", "AddMoneyByEmptyRooms;15", "", "VacancyEconomy|Support"),
            R(3010, "Vacancy Listing", "Cashes in on an empty unit the moment someone moves out.", "Card_Event", 0, 20, 0, nameof(CardPlayTargetKind.Room), 0, 0, "", "EvictTenantInSelectedRoom|AddMoneyByEmptyRooms;20", "", "", "VacancyEconomy|Payoff"),
            R(3011, "Discount Flyer", "Turns a quiet market into immediate card flow.", "Card_Event", 0, 0, 0, nameof(CardPlayTargetKind.PlayArea), 0, 0, "", "DrawCard;1|AddMoneyByEmptyRooms;10", "", "", "VacancyEconomy|Bridge"),
            R(4002, "Vacancy Clause", "A contract that pays better when units stay open.", "Card_Contract", 1, 100, 0, nameof(CardPlayTargetKind.PlayArea), 0, 4, "", "", "AddMoneyByEmptyRooms;25", "", "VacancyEconomy|Anchor"),
            R(1017, "Floor Planner", "Makes every extra room count a little more.", "Card_Tenant", 1, 65, 28, nameof(CardPlayTargetKind.Room), 0, 5, "", "", "AddMoneyByRoomCount;10", "", "Expansion|Core"),
            R(1018, "Renovation Lead", "Rewards the board for having more rooms in play.", "Card_Tenant", 2, 70, 32, nameof(CardPlayTargetKind.Room), 0, 5, "", "", "AddMoneyByRoomCount;12", "", "Expansion|Core"),
            R(2012, "Partition Wall", "Pure capacity growth with no extra conditions.", "Card_Equipt", 2, 110, 0, nameof(CardPlayTargetKind.Room), 0, 5, "", "ExpandSlot;2", "", "", "Expansion|Engine"),
            R(3012, "Floor Permit", "Adds one more room and replaces itself in hand.", "Card_Event", 1, 80, 0, nameof(CardPlayTargetKind.PlayArea), 0, 0, "", "AddRoom|DrawCard;1", "", "", "Expansion|Bridge"),
            R(4003, "Blueprint Deal", "A scaling contract that gets better as the property grows.", "Card_Contract", 2, 140, 0, nameof(CardPlayTargetKind.PlayArea), 0, 4, "", "", "AddMoneyByRoomCount;20|DrawCard;1", "", "Expansion|Anchor"),
            R(1019, "Night Auditor", "Pays more when the chosen room is full of tenants.", "Card_Tenant", 1, 50, 20, nameof(CardPlayTargetKind.Room), 0, 4, "", "", "AddMoneyBySelectedRoomTenantCount;20", "", "BurstSettlement|Core"),
            R(1020, "Flash Broker", "A sharp burst tenant that pays hard and leaves value behind.", "Card_Tenant", 2, 80, 25, nameof(CardPlayTargetKind.Room), 0, 3, "", "", "AddMoneyBySelectedRoomTenantCount;25", "AddMoney;70", "BurstSettlement|Payoff"),
            R(2013, "Turbo Meter", "Turns room density into one more spike every settlement.", "Card_Equipt", 2, 90, 0, nameof(CardPlayTargetKind.Room), 0, 4, "", "", "AddMoneyBySelectedRoomTenantCount;10|AddMoney;15", "", "BurstSettlement|Support"),
            R(3013, "Snap Settlement", "Triggers one safe extra settlement for the chosen room.", "Card_Event", 1, 70, 0, nameof(CardPlayTargetKind.Room), 0, 0, "", "TriggerSelectedRoomSettle", "", "", "BurstSettlement|Tempo"),
            R(4004, "Closing Statement", "A short contract that pushes a final money-and-cards burst.", "Card_Contract", 2, 130, 0, nameof(CardPlayTargetKind.PlayArea), 0, 3, "", "", "AddMoney;35|DrawCard;1", "", "BurstSettlement|Anchor"),
        };

        internal static readonly LibraryRow[] Libraries =
        {
            L("AllCards", "AllCards", "All Cards", AllCardIds()),
            L("FirstTurnPool", "FirstTurnPool", "First Turn Pool",
                1001, 1001, 1002, 1004, 1005, 1005, 1008, 1009, 1010,
                2001, 2002, 2003, 2003, 2005,
                3002, 3003, 3004,
                1011, 1013, 1015, 1016, 1017, 1017,
                2006, 2008, 2011, 2012,
                3006, 3007, 3009, 3011, 3012),
            L("NormalTurnPool", "NormalTurnPool", "Normal Turn Pool",
                1001, 1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009, 1010,
                1011, 1012, 1013, 1014, 1015, 1016, 1017, 1018, 1019, 1020,
                2001, 2002, 2003, 2004, 2005, 2006, 2007, 2008, 2009, 2010, 2011, 2012, 2013,
                3001, 3002, 3003, 3004, 3005, 3006, 3007, 3008, 3009, 3010, 3011, 3012, 3013,
                4001, 4002, 4003, 4004,
                1013, 1016, 1017, 2008, 2011, 2012, 3007, 3010, 3012, 3013),
            L("RewardPool", "RewardPool", "Reward Pool",
                1002, 1003, 1006, 1007, 1012, 1014, 1018, 1019, 1020,
                2002, 2004, 2006, 2008, 2009, 2010, 2011, 2012, 2013,
                3001, 3005, 3008, 3009, 3010, 3012, 3013,
                4001, 4002, 4003, 4004,
                2008, 2012, 3013, 4002, 4003, 4004, 1019, 1020),
        };

        private static CardRow R(
            int cardId,
            string cardName,
            string description,
            string cardType,
            int rarity,
            int cost,
            int baseRent,
            string targetKind,
            int waitTurns,
            int durability,
            string preEffect,
            string instantEffect,
            string settleEffect,
            string destroyEffect,
            string tag,
            string cardArt = "")
        {
            return new CardRow
            {
                CardId = cardId,
                CardName = cardName,
                Description = description,
                CardType = cardType,
                Rarity = rarity,
                CardArt = cardArt,
                Cost = cost,
                BaseRent = baseRent,
                TargetKind = targetKind,
                WaitTurns = waitTurns,
                Durability = durability,
                PreEffect = preEffect,
                InstantEffect = instantEffect,
                SettleEffect = settleEffect,
                DestroyEffect = destroyEffect,
                Tag = tag
            };
        }

        private static LibraryRow L(string assetName, string libraryId, string displayName, params int[] cardIds)
        {
            return new LibraryRow
            {
                AssetName = assetName,
                LibraryId = libraryId,
                DisplayName = displayName,
                CardIds = cardIds
            };
        }

        private static int[] AllCardIds()
        {
            return Rows.Select(row => row.CardId).ToArray();
        }
    }
}
