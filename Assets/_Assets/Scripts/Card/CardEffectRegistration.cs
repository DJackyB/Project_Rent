using BaoZuPo.Card.Effects;

namespace BaoZuPo.Card
{
    public static class CardEffectRegistration
    {
        public static void EnsureRegistered()
        {
            CardEffectFactory.Register("AddMoney", 1, args => new AddMoneyEffect(int.Parse(args[0])));
            CardEffectFactory.Register("ReduceMoney", 1, args => new ReduceMoneyEffect(int.Parse(args[0])));
            CardEffectFactory.Register("DrawCard", 1, args => new DrawCardEffect(int.Parse(args[0]), args.Length > 1 ? args[1] : null));
            CardEffectFactory.Register("ExpandSlot", 1, args => new ExpandSlotEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddTenantDurability", 1, args => new AddTenantDurabilityEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddEquipmentDurability", 1, args => new AddEquipmentDurabilityEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddMoneyByEmptyRooms", 1, args => new AddMoneyByEmptyRoomsEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddMoneyByRoomCount", 1, args => new AddMoneyByRoomCountEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddMoneyBySelectedRoomTenantCount", 1, args => new AddMoneyBySelectedRoomTenantCountEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddTenantDurabilityInSelectedRoom", 1, args => new AddTenantDurabilityInSelectedRoomEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddRoom", _ => new AddRoomEffect());
            CardEffectFactory.Register("MoveTenantToEmptyRoom", _ => new MoveTenantToEmptyRoomEffect());
            CardEffectFactory.Register("EvictTenantInSelectedRoom", _ => new EvictTenantInSelectedRoomEffect());
            CardEffectFactory.Register("TriggerSelectedRoomSettle", _ => new TriggerSelectedRoomSettleEffect());
            CardEffectFactory.Register("SpawnRandomTenantInSelectedRoom", _ => new SpawnRandomTenantInSelectedRoomEffect());
        }
    }
}
