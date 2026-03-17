using UnityEngine;
using BaoZuPo.Card;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 额外抽牌效果
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
            Deck.DeckManager.Instance.Draw(_count);
            Debug.Log($"[效果] 额外抽牌 {_count} 张");
        }
    }
}
