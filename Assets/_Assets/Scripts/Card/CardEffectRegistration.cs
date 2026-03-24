using BaoZuPo.Card.Effects;

namespace BaoZuPo.Card
{
    public static class CardEffectRegistration
    {
        public static void EnsureRegistered()
        {
            CardEffectFactory.Register("AddMoney", args => new AddMoneyEffect(int.Parse(args[0])));
            CardEffectFactory.Register("ReduceMoney", args => new ReduceMoneyEffect(int.Parse(args[0])));
            CardEffectFactory.Register("DrawCard", args => new DrawCardEffect(int.Parse(args[0])));
            CardEffectFactory.Register("ExpandSlot", args => new ExpandSlotEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddTenantDurability", args => new AddTenantDurabilityEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddEquipmentDurability", args => new AddEquipmentDurabilityEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddMoneyByEmptyRooms", args => new AddMoneyByEmptyRoomsEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddMoneyByRoomCount", args => new AddMoneyByRoomCountEffect(int.Parse(args[0])));
            CardEffectFactory.Register("AddTenantDurabilityInSelectedRoom", args => new AddTenantDurabilityInSelectedRoomEffect(int.Parse(args[0])));
            CardEffectFactory.Register("MoveTenantToEmptyRoom", _ => new MoveTenantToEmptyRoomEffect());
            CardEffectFactory.Register("EvictTenantInSelectedRoom", _ => new EvictTenantInSelectedRoomEffect());
            CardEffectFactory.Register("TriggerSelectedRoomSettle", _ => new TriggerSelectedRoomSettleEffect());
            CardEffectFactory.Register("SpawnRandomTenantInSelectedRoom", _ => new SpawnRandomTenantInSelectedRoomEffect());
        }
    }
}
