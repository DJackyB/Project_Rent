using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 立即结算一次选中房间（不扣耐久）
    /// 格式：TriggerSelectedRoomSettle
    /// </summary>
    public class TriggerSelectedRoomSettleEffect : ICardEffect
    {
        public void Execute(CardInstance source, GameContext context)
        {
            var room = context.EffectContext.SelectedRoom;
            if (room == null)
            {
                Debug.LogWarning($"[效果] {source.Data.cardName}: 未选择房间，效果跳过");
                return;
            }

            if (room.TenantCount <= 0)
            {
                Debug.LogWarning($"[效果] {source.Data.cardName}: 房间{room.RoomIndex}无租客，不触发结算");
                return;
            }

            foreach (var card in room.GetAllCards())
            {
                if (card.IsDestroyed) continue;
                card.SettleEffect?.Execute(card, context);
            }

            Debug.Log($"[效果] {source.Data.cardName}: 房间{room.RoomIndex} 立即结算一次");
        }
    }
}
