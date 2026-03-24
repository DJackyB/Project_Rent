using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 对选中房间额外触发一次结算，但不扣减耐久。
    /// 格式：TriggerSelectedRoomSettle
    /// </summary>
    public class TriggerSelectedRoomSettleEffect : ICardEffect
    {
        public void Execute(CardInstance source, GameContext context)
        {
            var room = context.EffectContext.SelectedRoom;
            if (room == null)
            {
                Debug.LogWarning($"[Effect] {source.Data.cardName}: No room selected, effect skipped.");
                return;
            }

            if (room.TenantCount <= 0)
            {
                Debug.LogWarning($"[Effect] {source.Data.cardName}: Room {room.RoomIndex} has no tenant, settle skipped.");
                return;
            }

            foreach (var card in room.GetAllCards())
            {
                if (card.IsDestroyed) continue;
                card.SettleEffect?.Execute(card, context);
            }

            Debug.Log($"[Effect] {source.Data.cardName}: Triggered settle once for room {room.RoomIndex}.");
        }
    }
}
