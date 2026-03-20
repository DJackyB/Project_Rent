using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 在选中房间生成一名随机租客（从卡库租客池）
    /// 格式：SpawnRandomTenantInSelectedRoom
    /// </summary>
    public class SpawnRandomTenantInSelectedRoomEffect : ICardEffect
    {
        public void Execute(CardInstance source, GameContext context)
        {
            var room = context.EffectContext.SelectedRoom;
            if (room == null || !room.CanPlaceTenant)
            {
                Debug.LogWarning($"[效果] {source.Data.cardName}: 目标房间不可入住");
                return;
            }

            var pool = new List<CardData>();
            foreach (var kv in CardDatabase.GetAll())
            {
                if (kv.Value.cardType == CardType.Tenant)
                    pool.Add(kv.Value);
            }

            if (pool.Count == 0)
            {
                Debug.LogWarning($"[效果] {source.Data.cardName}: 租客池为空");
                return;
            }

            var pick = pool[Random.Range(0, pool.Count)];
            var tenant = new CardInstance(pick);
            room.PlaceCard(tenant);
            tenant.InstantEffect?.Execute(tenant, context);
            Debug.Log($"[效果] {source.Data.cardName}: 房间{room.RoomIndex} 新入住 {tenant.Data.cardName}");
        }
    }
}
