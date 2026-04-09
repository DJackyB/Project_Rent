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

            // 递归防护：检查是否已在额外结算上下文中，防止嵌套触发此效果导致无限递归。
            if (context.EffectContext.IsExtraRoomSettlementActive)
            {
                Debug.LogWarning($"[Effect] {source.Data.cardName}: Nested extra settlement is blocked.");
                return;
            }

            // 额外结算只重放本房间收益逻辑，不再扣耐久，也不允许再次触发额外结算。
            // 通过设置 IsExtraRoomSettlementActive 标志来进入受保护上下文，防止递归。
            context.EffectContext.IsExtraRoomSettlementActive = true;
            try
            {
                // 遍历房间内所有租客，仅执行 SettleEffect（不扣耐久）。
                foreach (var tenant in room.GetTenants())
                {
                    if (tenant == null || tenant.IsDestroyed)
                    {
                        continue;
                    }

                    // 收集基础租金收入。
                    int baseRent = Mathf.Max(0, tenant.Data != null ? tenant.Data.baseRent : 0);
                    if (baseRent > 0)
                    {
                        context.MoneyManager.AddMoney(baseRent);
                        if (context.SettlementCapture.IsCapturing)
                        {
                            context.SettlementCapture.RecordBase(baseRent, GameText.SettlementBase);
                        }
                    }

                    // 若租客的 SettleEffect 中包含 TriggerSelectedRoomSettle，则跳过此租客的 SettleEffect，避免递归。
                    if (ShouldSkipExtraSettlementEffect(tenant))
                    {
                        continue;
                    }

                    tenant.SettleEffect?.Execute(tenant, context);
                }

                // 遍历房间内所有装备，仅执行 SettleEffect（同样防止递归）。
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
                // 无论成功还是异常，都必须清除标志，恢复上下文以允许后续操作。
                context.EffectContext.IsExtraRoomSettlementActive = false;
            }

            Debug.Log($"[Effect] {source.Data.cardName}: Triggered extra settle once for room {room.RoomIndex} without durability loss.");
        }

        // 检查卡牌的 SettleEffect 是否包含 TriggerSelectedRoomSettle，若包含则在额外结算上下文中跳过此卡牌，防止递归。
        private static bool ShouldSkipExtraSettlementEffect(CardInstance card)
        {
            return card != null
                && card.Data != null
                && !string.IsNullOrWhiteSpace(card.Data.settleEffect)
                && card.Data.settleEffect.Contains("TriggerSelectedRoomSettle");
        }
    }
}
