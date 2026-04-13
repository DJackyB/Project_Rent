using BaoZuPo.Card.Effects;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌效果注册表初始化。
    ///
    /// 在游戏启动时调用 EnsureRegistered()，将所有内置效果注册到 CardEffectFactory。
    /// 每个效果都由一个效果类和一个工厂委托实现。
    ///
    /// 效果格式规范：
    /// - 无参数效果：只需效果 ID，如 "AddRoom"
    /// - 单参数效果：格式 "EffectId;param"，如 "AddMoney;100"
    /// - 多参数效果：格式 "EffectId;param1;param2"，如 "DrawCard;2;EventPool"
    /// - 复合效果：多个效果用 | 分隔，如 "AddMoney;50 | DrawCard;1"
    /// </summary>
    public static class CardEffectRegistration
    {
        public static bool IsRegistered { get; private set; }

        /// <summary>
        /// 初始化并注册所有内置效果。
        ///
        /// 应在游戏启动时调用一次（如 GameBootstrap 中）。
        /// 在此之后，CardEffectFactory 可以解析效果字符串。
        /// </summary>
        public static void EnsureRegistered()
        {
            // ===== 金钱效果 =====
            // AddMoney;100 - 增加 100 金钱
            CardEffectFactory.Register("AddMoney", 1, args => new AddMoneyEffect(int.Parse(args[0])));

            // ReduceMoney;50 - 减少 50 金钱
            CardEffectFactory.Register("ReduceMoney", 1, args => new ReduceMoneyEffect(int.Parse(args[0])));

            // ===== 卡牌相关效果 =====
            // DrawCard;2 - 从默认卡池抽 2 张
            // DrawCard;2;EventPool - 从 EventPool 卡库抽 2 张
            CardEffectFactory.Register("DrawCard", 1, args => new DrawCardEffect(int.Parse(args[0]), args.Length > 1 ? args[1] : null));

            // ===== 房间相关效果 =====
            // ExpandSlot;1 - 增加 1 个房间槽位
            CardEffectFactory.Register("ExpandSlot", 1, args => new ExpandSlotEffect(int.Parse(args[0])));

            // AddRoom - 增加 1 个房间（无参数）
            CardEffectFactory.Register("AddRoom", _ => new AddRoomEffect());

            // ===== 租客耐久度效果 =====
            // AddTenantDurability;2 - 为所有租客增加 2 耐久度
            CardEffectFactory.Register("AddTenantDurability", 1, args => new AddTenantDurabilityEffect(int.Parse(args[0])));

            // AddTenantDurabilityInSelectedRoom;1 - 为选中房间的租客增加 1 耐久度
            CardEffectFactory.Register("AddTenantDurabilityInSelectedRoom", 1, args => new AddTenantDurabilityInSelectedRoomEffect(int.Parse(args[0])));

            // ===== 装备耐久度效果 =====
            // AddEquipmentDurability;1 - 为所有装备增加 1 耐久度
            CardEffectFactory.Register("AddEquipmentDurability", 1, args => new AddEquipmentDurabilityEffect(int.Parse(args[0])));

            // ===== 条件金钱效果 =====
            // AddMoneyByEmptyRooms;10 - 根据空房间数，每个空房间增加 10 金钱
            CardEffectFactory.Register("AddMoneyByEmptyRooms", 1, args => new AddMoneyByEmptyRoomsEffect(int.Parse(args[0])));

            // AddMoneyByRoomCount;5 - 根据房间总数，每个房间增加 5 金钱
            CardEffectFactory.Register("AddMoneyByRoomCount", 1, args => new AddMoneyByRoomCountEffect(int.Parse(args[0])));

            // AddMoneyBySelectedRoomTenantCount;20 - 根据选中房间的租客数，每个租客增加 20 金钱
            CardEffectFactory.Register("AddMoneyBySelectedRoomTenantCount", 1, args => new AddMoneyBySelectedRoomTenantCountEffect(int.Parse(args[0])));

            // ===== 高级房间操作 =====
            // MoveTenantToEmptyRoom - 将选中房间的租客移动到空房间（无参数）
            CardEffectFactory.Register("MoveTenantToEmptyRoom", _ => new MoveTenantToEmptyRoomEffect());

            // EvictTenantInSelectedRoom - 驱赶选中房间的租客（无参数）
            CardEffectFactory.Register("EvictTenantInSelectedRoom", _ => new EvictTenantInSelectedRoomEffect());

            // TriggerSelectedRoomSettle - 立即触发选中房间的结算（无参数）
            // 注意：防止递归，由 EffectExecutionContext.IsExtraRoomSettlementActive 保护
            CardEffectFactory.Register("TriggerSelectedRoomSettle", _ => new TriggerSelectedRoomSettleEffect());

            // SpawnRandomTenantInSelectedRoom - 在选中房间生成一个随机租客（无参数）
            CardEffectFactory.Register("SpawnRandomTenantInSelectedRoom", _ => new SpawnRandomTenantInSelectedRoomEffect());

            // ===== 随机事件效果 =====
            // TriggerRandomEvent;act1_events - 从指定库中加权随机触发一个随机事件
            CardEffectFactory.Register("TriggerRandomEvent", 1, args => new TriggerRandomEventEffect(args[0]));

            IsRegistered = true;
        }

        public static void MarkUnregisteredForTesting()
        {
            IsRegistered = false;
        }
    }
}
