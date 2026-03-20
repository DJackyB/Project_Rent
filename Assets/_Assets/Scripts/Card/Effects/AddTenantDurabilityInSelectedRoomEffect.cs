using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 给选中房间内所有租客加耐久
    /// 格式：AddTenantDurabilityInSelectedRoom;数值
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
                Debug.LogWarning($"[效果] {source.Data.cardName}: 未选择房间，效果跳过");
                return;
            }

            int affected = 0;
            foreach (var tenant in room.GetTenants())
            {
                if (tenant.IsDestroyed || tenant.Data.durability <= 0) continue;
                tenant.CurrentDurability += _amount;
                affected++;
            }

            Debug.Log($"[效果] {source.Data.cardName}: 房间{room.RoomIndex} 租客耐久 {_amount:+#;-#;0}（{affected} 张）");
        }
    }
}
