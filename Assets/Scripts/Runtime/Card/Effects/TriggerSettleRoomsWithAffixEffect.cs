using BaoZuPo.UI;
using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 对场上所有含指定词条租客的房间额外触发一次结算，不扣耐久。
    /// 格式：TriggerSettleRoomsWithAffix;Quiet
    /// </summary>
    public class TriggerSettleRoomsWithAffixEffect : ICardEffect
    {
        private readonly TagType _affix;

        public TriggerSettleRoomsWithAffixEffect(TagType affix)
        {
            _affix = affix;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            if (context.EffectContext.IsExtraRoomSettlementActive)
            {
                Debug.LogWarning($"[Effect] {EffectSourceHelper.Name(source)}: Nested extra settlement is blocked.");
                return;
            }

            var rooms = context.BoardManager.GetAllRooms();
            context.EffectContext.IsExtraRoomSettlementActive = true;
            try
            {
                foreach (var room in rooms)
                {
                    if (room == null || room.TenantCount <= 0 || !TagQuery.RoomHasTag(room, _affix))
                    {
                        continue;
                    }

                    foreach (var tenant in room.GetTenants())
                    {
                        if (tenant == null || tenant.IsDestroyed)
                        {
                            continue;
                        }

                        int baseRent = tenant.Data != null ? UnityEngine.Mathf.Max(0, tenant.Data.baseRent) : 0;
                        if (baseRent > 0)
                        {
                            context.MoneyManager.AddMoney(baseRent);
                            if (context.SettlementCapture.IsCapturing)
                            {
                                context.SettlementCapture.RecordBase(baseRent, GameText.SettlementBase);
                            }
                        }

                        if (ShouldSkip(tenant))
                        {
                            continue;
                        }

                        tenant.SettleEffect?.Execute(tenant, context);
                    }

                    foreach (var equipment in room.GetEquipments())
                    {
                        if (equipment == null || equipment.IsDestroyed || ShouldSkip(equipment))
                        {
                            continue;
                        }

                        equipment.SettleEffect?.Execute(equipment, context);
                    }

                    Debug.Log($"[Effect] {EffectSourceHelper.Name(source)}: Extra settle for room {room.RoomIndex}");
                }
            }
            finally
            {
                context.EffectContext.IsExtraRoomSettlementActive = false;
            }
        }

        private static bool ShouldSkip(CardInstance card)
        {
            return card?.Data != null
                && CardEffectFactory.ContainsEffect(card.Data.settleEffect, "TriggerSettleRoomsWithAffix");
        }
    }
}
