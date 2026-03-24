using BaoZuPo.Board;
using BaoZuPo.Card;
using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 扩充卡牌所在房间的租客槽位。
    /// 格式：ExpandSlot;数量
    /// </summary>
    public class ExpandSlotEffect : ICardEffect
    {
        private readonly int _count;

        public ExpandSlotEffect(int count)
        {
            _count = count;
        }

        public void Execute(CardInstance card, GameContext context)
        {
            if (card.PlacedRoom != null)
            {
                card.PlacedRoom.ExpandTenantSlots(_count);
                Debug.Log($"[Effect] Room {card.PlacedRoom.RoomIndex} tenant slots +{_count}.");
            }
            else
            {
                Debug.LogWarning("[Effect] ExpandSlot: card is not placed in a room, effect skipped.");
            }
        }
    }
}
