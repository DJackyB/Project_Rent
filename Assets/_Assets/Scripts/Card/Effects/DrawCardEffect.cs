using BaoZuPo.Card;
using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 抽取额外卡牌。
    /// 格式：DrawCard;数量
    /// </summary>
    public class DrawCardEffect : ICardEffect
    {
        private readonly int _count;

        public DrawCardEffect(int count)
        {
            _count = count;
        }

        public void Execute(CardInstance card, GameContext context)
        {
            context.DeckManager.Draw(_count);
            Debug.Log($"[Effect] Drew {_count} extra cards.");
        }
    }
}
