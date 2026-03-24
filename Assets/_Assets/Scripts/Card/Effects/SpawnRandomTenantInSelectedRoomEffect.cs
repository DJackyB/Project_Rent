using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 从卡牌数据库中随机生成一张租客卡，放入选中房间。
    /// 格式：SpawnRandomTenantInSelectedRoom
    /// </summary>
    public class SpawnRandomTenantInSelectedRoomEffect : ICardEffect
    {
        public void Execute(CardInstance source, GameContext context)
        {
            var room = context.EffectContext.SelectedRoom;
            if (room == null || !room.CanPlaceTenant)
            {
                Debug.LogWarning($"[Effect] {source.Data.cardName}: Target room cannot accept tenants.");
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
                Debug.LogWarning($"[Effect] {source.Data.cardName}: Tenant pool is empty.");
                return;
            }

            var pick = pool[Random.Range(0, pool.Count)];
            var tenant = new CardInstance(pick);
            room.PlaceCard(tenant);
            tenant.InstantEffect?.Execute(tenant, context);
            Debug.Log($"[Effect] {source.Data.cardName}: Spawned tenant {tenant.Data.cardName} in room {room.RoomIndex}.");
        }
    }
}
