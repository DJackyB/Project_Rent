using System.Globalization;
using BaoZuPo.Card.Effects;

namespace BaoZuPo.Card
{
    /// <summary>
    /// Registers built-in card effects into the runtime factory.
    /// </summary>
    public static class CardEffectRegistration
    {
        public static bool IsRegistered { get; private set; }

        public static void EnsureRegistered()
        {
            if (IsRegistered) return;

            CardEffectFactory.Register("AddMoney", 1, args => new AddMoneyEffect(int.Parse(args[0])));
            CardEffectFactory.Register("ReduceMoney", 1, args => new ReduceMoneyEffect(int.Parse(args[0])));
            CardEffectFactory.Register("DrawCard", 1, args => new DrawCardEffect(int.Parse(args[0]), args.Length > 1 ? args[1] : null));
            CardEffectFactory.Register("OpenShop", _ => new OpenShopEffect());

            CardEffectFactory.Register("ExpandSlot", 1, args => new ExpandSlotEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddRoom", _ => new AddRoomEffect());

            CardEffectFactory.Register("AddTenantDurability", 1, args => new AddTenantDurabilityEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddTenantDurabilityInSelectedRoom", 1, args => new AddTenantDurabilityInSelectedRoomEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddEquipmentDurability", 1, args => new AddEquipmentDurabilityEffect(int.Parse(args[0])));

            CardEffectFactory.Register("AddMoneyByEmptyRooms", 1, args => new AddMoneyByEmptyRoomsEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddMoneyByRoomCount", 1, args => new AddMoneyByRoomCountEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddMoneyBySelectedRoomTenantCount", 1, args => new AddMoneyBySelectedRoomTenantCountEffect(int.Parse(args[0])));

            CardEffectFactory.Register("MoveTenantToEmptyRoom", _ => new MoveTenantToEmptyRoomEffect());
            CardEffectFactory.Register("EvictTenantInSelectedRoom", _ => new EvictTenantInSelectedRoomEffect());
            CardEffectFactory.Register("TriggerSelectedRoomSettle", _ => new TriggerSelectedRoomSettleEffect());
            CardEffectFactory.Register("SpawnRandomTenantInSelectedRoom", _ => new SpawnRandomTenantInSelectedRoomEffect());

            // ===== 随机事件效果 =====
            // TriggerRandomEvent;act1_events - 从指定库中加权随机触发一个随机事件
            CardEffectFactory.Register("TriggerRandomEvent", 1, args => new TriggerRandomEventEffect(args[0]));

            // ===== 词条效果 =====
            // AddMoneyIfAffixInRoom;Quiet;200 - 房间有词条租客时增加固定金额
            CardEffectFactory.Register("AddMoneyIfAffixInRoom", 2, args =>
                new AddMoneyIfAffixInRoomEffect(ParseTag(args[0]), int.Parse(args[1])));

            // AddMoneyIfAdjacentAffix;Quiet;200 - 相邻房间有词条租客时增加固定金额
            CardEffectFactory.Register("AddMoneyIfAdjacentAffix", 2, args =>
                new AddMoneyIfAdjacentAffixEffect(ParseTag(args[0]), int.Parse(args[1])));

            // MultiplyIfAffixInRoom;Quiet;1.5 - 房间有词条租客时对词条租客 baseRent 施加乘值
            CardEffectFactory.Register("MultiplyIfAffixInRoom", 2, args =>
                new MultiplyIfAffixInRoomEffect(ParseTag(args[0]), float.Parse(args[1], CultureInfo.InvariantCulture)));

            // MultiplyIfAllAdjacentAffix;Quiet;2.0 - 所有相邻房间均有词条租客时施加乘值
            CardEffectFactory.Register("MultiplyIfAllAdjacentAffix", 2, args =>
                new MultiplyIfAllAdjacentAffixEffect(ParseTag(args[0]), float.Parse(args[1], CultureInfo.InvariantCulture)));

            // AddMoneyPerAffixOnBoard;Quiet;200 - 按场上词条租客总数增加金额
            CardEffectFactory.Register("AddMoneyPerAffixOnBoard", 2, args =>
                new AddMoneyPerAffixOnBoardEffect(ParseTag(args[0]), int.Parse(args[1])));

            // AffixRoomMultiplier;Quiet;1.5 - 对含词条租客的每个房间施加乘值（合同用）
            CardEffectFactory.Register("AffixRoomMultiplier", 2, args =>
                new AffixRoomMultiplierEffect(ParseTag(args[0]), float.Parse(args[1], CultureInfo.InvariantCulture)));

            // TriggerSettleRoomsWithAffix;Quiet - 对所有含词条租客的房间额外结算一次
            CardEffectFactory.Register("TriggerSettleRoomsWithAffix", 1, args =>
                new TriggerSettleRoomsWithAffixEffect(ParseTag(args[0])));

            // ExtraDurabilityChance;0.3 - 结算时按概率额外损耗 1 耐久
            CardEffectFactory.Register("ExtraDurabilityChance", 1, args =>
                new ExtraDurabilityChanceEffect(float.Parse(args[0])));

            IsRegistered = true;
        }

        private static TagType ParseTag(string s)
        {
            return (TagType)System.Enum.Parse(typeof(TagType), s, ignoreCase: true);
        }

        public static void MarkUnregisteredForTesting()
        {
            IsRegistered = false;
        }
    }
}
