using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 按当前选中房间的租客数量结算金钱。
    /// 格式：AddMoneyBySelectedRoomTenantCount;每名租客金额
    /// </summary>
    public class AddMoneyBySelectedRoomTenantCountEffect : ICardEffect
    {
        private readonly int _amountPerTenant;

        public AddMoneyBySelectedRoomTenantCountEffect(int amountPerTenant)
        {
            _amountPerTenant = amountPerTenant;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            var room = context?.EffectContext?.SelectedRoom;
            if (room == null)
            {
                Debug.LogWarning($"[Effect] {source?.Data?.cardName ?? "Unknown"}: No room selected, effect skipped.");
                return;
            }

            int tenantCount = room.TenantCount;
            int total = tenantCount * _amountPerTenant;
            if (total != 0)
            {
                context.MoneyManager.AddMoney(total);
                context?.SettlementCapture?.RecordDelta(total, source != null && source.Data != null ? source.Data.cardName : null);
            }

            Debug.Log($"[Effect] {source?.Data?.cardName ?? "Unknown"}: Room {room.RoomIndex} tenant count {tenantCount}, money delta {total}.");
        }
    }
}
