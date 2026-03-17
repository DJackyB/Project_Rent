using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 运行时卡牌实例
    /// 持有 CardData 的引用（静态数据）以及运行时可变状态（耐久、等待等）。
    /// 纯 C# 类，不继承 MonoBehaviour。
    /// </summary>
    public class CardInstance
    {
        /// <summary>静态数据引用</summary>
        public CardData Data { get; private set; }

        /// <summary>当前耐久值（0 表示无限耐久）</summary>
        public int CurrentDurability { get; set; }

        /// <summary>当前等待倒计时（0 表示无等待）</summary>
        public int CurrentWait { get; set; }

        /// <summary>所在房间槽位（null 表示在手牌中或卡组中）</summary>
        public Board.RoomSlot PlacedRoom { get; set; }

        /// <summary>是否已被销毁</summary>
        public bool IsDestroyed { get; private set; }

        // 已解析的效果实例（从 CardData 的效果字符串解析而来）
        public ICardEffect PreEffect { get; private set; }
        public ICardEffect InstantEffect { get; private set; }
        public ICardEffect SettleEffect { get; private set; }
        public ICardEffect DestroyEffect { get; private set; }

        /// <summary>
        /// 从 CardData 创建运行时实例
        /// </summary>
        public CardInstance(CardData data)
        {
            Data = data;
            CurrentDurability = data.durability;
            CurrentWait = data.waitTurns;
            IsDestroyed = false;

            // 解析效果字符串为 ICardEffect 实例
            PreEffect = CardEffectFactory.Create(data.preEffect);
            InstantEffect = CardEffectFactory.Create(data.instantEffect);
            SettleEffect = CardEffectFactory.Create(data.settleEffect);
            DestroyEffect = CardEffectFactory.Create(data.destroyEffect);
        }

        /// <summary>
        /// 标记为已销毁
        /// </summary>
        public void MarkDestroyed()
        {
            IsDestroyed = true;
        }

        public override string ToString()
        {
            return $"[{Data.cardName}](ID:{Data.cardId}, 耐久:{CurrentDurability}, 等待:{CurrentWait})";
        }
    }
}
