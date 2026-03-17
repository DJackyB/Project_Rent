using UnityEngine;
using BaoZuPo.Card;
using BaoZuPo.Board;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 扩展房间租客槽位
    /// 格式：ExpandSlot;数量
    /// 放入房间时，将该房间的租客槽位扩展
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
                Debug.Log($"[效果] 房间{card.PlacedRoom.RoomIndex} 租客槽位 +{_count}");
            }
            else
            {
                Debug.LogWarning("[效果] ExpandSlot：卡牌未放入房间，效果无法生效");
            }
        }
    }
}
