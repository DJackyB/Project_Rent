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
        // 执行：验证选中房间可容纳租客，收集数据库中所有租客卡牌，随机抽一张，创建实例，放入房间，触发 InstantEffect。
        public void Execute(CardInstance source, GameContext context)
        {
            var room = context.EffectContext.SelectedRoom;
            if (room == null || !room.CanPlaceTenant)
            {
                Debug.LogWarning($"[Effect] {EffectSourceHelper.Name(source)}: Target room cannot accept tenants.");
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
                Debug.LogWarning($"[Effect] {EffectSourceHelper.Name(source)}: Tenant pool is empty.");
                return;
            }

            var pick = pool[Random.Range(0, pool.Count)];
            var tenant = new CardInstance(pick);
            room.PlaceCard(tenant);
            tenant.InstantEffect?.Execute(tenant, context);
            Debug.Log($"[Effect] {EffectSourceHelper.Name(source)}: Spawned tenant {tenant.Data.cardName} in room {room.RoomIndex}.");
        }
    }
}
