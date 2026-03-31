using BaoZuPo.UI;
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

            if (context.EffectContext.IsExtraRoomSettlementActive)
            {
                Debug.LogWarning($"[Effect] {source.Data.cardName}: Nested extra settlement is blocked.");
                return;
            }

            // 额外结算只重放本房间收益逻辑，不再扣耐久，也不允许再次触发额外结算。
            context.EffectContext.IsExtraRoomSettlementActive = true;
            try
            {
                foreach (var tenant in room.GetTenants())
                {
                    if (tenant == null || tenant.IsDestroyed)
                    {
                        continue;
                    }

                    int baseRent = Mathf.Max(0, tenant.Data != null ? tenant.Data.baseRent : 0);
                    if (baseRent > 0)
                    {
                        context.MoneyManager.AddMoney(baseRent);
                        context.SettlementCapture.RecordBase(baseRent, GameText.SettlementBase);
                    }

                    if (ShouldSkipExtraSettlementEffect(tenant))
                    {
                        continue;
                    }

                    tenant.SettleEffect?.Execute(tenant, context);
                }

                foreach (var equipment in room.GetEquipments())
                {
                    if (equipment == null || equipment.IsDestroyed || ShouldSkipExtraSettlementEffect(equipment))
                    {
                        continue;
                    }

                    equipment.SettleEffect?.Execute(equipment, context);
                }
            }
            finally
            {
                context.EffectContext.IsExtraRoomSettlementActive = false;
            }

            Debug.Log($"[Effect] {source.Data.cardName}: Triggered extra settle once for room {room.RoomIndex} without durability loss.");
        }

        private static bool ShouldSkipExtraSettlementEffect(CardInstance card)
        {
            return card != null
                && card.Data != null
                && !string.IsNullOrWhiteSpace(card.Data.settleEffect)
                && card.Data.settleEffect.Contains("TriggerSelectedRoomSettle");
        }
    }
}
