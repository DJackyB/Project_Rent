using BaoZuPo.Localization;
using UnityEngine;

namespace BaoZuPo.Card
{
    public class CardInstance
    {
        public CardData Data { get; private set; }
        public int CurrentDurability { get; set; }
        public int CurrentWait { get; set; }
        public Board.RoomSlot PlacedRoom { get; set; }
        public bool IsDestroyed { get; private set; }

        public ICardEffect PreEffect { get; private set; }
        public ICardEffect InstantEffect { get; private set; }
        public ICardEffect SettleEffect { get; private set; }
        public ICardEffect DestroyEffect { get; private set; }

        public CardInstance(CardData data)
        {
            Data = data;
            CurrentDurability = data.durability;
            CurrentWait = data.waitTurns;
            IsDestroyed = false;

            PreEffect = CardEffectFactory.Create(data.preEffect);
            InstantEffect = CardEffectFactory.Create(data.instantEffect);
            SettleEffect = CardEffectFactory.Create(data.settleEffect);
            DestroyEffect = CardEffectFactory.Create(data.destroyEffect);
        }

        public void MarkDestroyed()
        {
            IsDestroyed = true;
        }

        public override string ToString()
        {
            return $"[{CardTextResolver.ResolveName(Data)}](ID:{Data.cardId}, 耐久:{CurrentDurability}, 等待:{CurrentWait})";
        }
    }
}
