using System;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 运行时卡牌实例，持有动态状态和已解析的效果对象。
    /// </summary>
    public class CardInstance
    {
        public CardData Data { get; private set; }
        public int CurrentDurability { get; set; }
        public int CurrentWait { get; set; }
        public Board.RoomSlot PlacedRoom { get; set; }
        public bool IsDestroyed { get; private set; }

        // 运行时元数据：用于像商店牌这种“只存在于当前回合手牌中”的临时卡。
        public bool IsTemporaryHandCard { get; private set; }
        public bool RemoveFromGameAfterPlay { get; private set; }
        public bool RemoveFromHandAtTurnEnd { get; private set; }

        public ICardEffect PreEffect { get; private set; }
        public ICardEffect InstantEffect { get; private set; }
        public ICardEffect SettleEffect { get; private set; }
        public ICardEffect DestroyEffect { get; private set; }

        public CardInstance(CardData data)
        {
            if (!CardEffectRegistration.IsRegistered)
            {
                throw new InvalidOperationException("[CardInstance] Card effects are not registered. Run CardEffectRegistration.EnsureRegistered() during game initialization before creating card instances.");
            }

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

        public void ConfigureAsTemporaryHandCard()
        {
            IsTemporaryHandCard = true;
            RemoveFromGameAfterPlay = true;
            RemoveFromHandAtTurnEnd = true;
        }

        public override string ToString()
        {
            return $"[{Data.cardName}](ID:{Data.cardId}, Durability:{CurrentDurability}, Wait:{CurrentWait})";
        }
    }
}
