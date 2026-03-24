using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 为选中房间中的所有租客增加耐久。
    /// 格式：AddTenantDurabilityInSelectedRoom;数量
    /// </summary>
    public class AddTenantDurabilityInSelectedRoomEffect : ICardEffect
    {
        private readonly int _amount;

        public AddTenantDurabilityInSelectedRoomEffect(int amount)
        {
            _amount = amount;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            var room = context.EffectContext.SelectedRoom;
            if (room == null)
            {
                Debug.LogWarning($"[Effect] {source.Data.cardName}: No room selected, effect skipped.");
                return;
            }

            int affected = 0;
            foreach (var tenant in room.GetTenants())
            {
                if (tenant.IsDestroyed || tenant.Data.durability <= 0) continue;
                tenant.CurrentDurability += _amount;
                affected++;
            }

            Debug.Log($"[Effect] {source.Data.cardName}: Room {room.RoomIndex} tenant durability {_amount:+#;-#;0} ({affected} cards).");
        }
    }
}
