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

        // 执行：获取选中房间，遍历其中所有未销毁且具有耐久属性的租客，为其增加耐久值。
        public void Execute(CardInstance source, GameContext context)
        {
            var room = context.EffectContext.SelectedRoom;
            if (room == null)
            {
                Debug.LogWarning($"[Effect] {EffectSourceHelper.Name(source)}: No room selected, effect skipped.");
                return;
            }

            int affected = 0;
            foreach (var tenant in room.GetTenants())
            {
                if (tenant.IsDestroyed || tenant.Data.durability <= 0) continue;
                tenant.CurrentDurability += _amount;
                affected++;
            }

            Debug.Log($"[Effect] {EffectSourceHelper.Name(source)}: Room {room.RoomIndex} tenant durability {_amount:+#;-#;0} ({affected} cards).");
        }
    }
}
