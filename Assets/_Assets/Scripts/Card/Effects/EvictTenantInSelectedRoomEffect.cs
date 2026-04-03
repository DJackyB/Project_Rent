using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 驱逐选中房间中的首个租客。
    /// 格式：EvictTenantInSelectedRoom
    /// </summary>
    public class EvictTenantInSelectedRoomEffect : ICardEffect
    {
        // 执行：检查选中房间是否有租客，若有则驱逐首个未销毁的租客（触发 DestroyEffect，标记为已销毁）。
        public void Execute(CardInstance source, GameContext context)
        {
            var room = context.EffectContext.SelectedRoom;
            if (room == null || room.TenantCount <= 0)
            {
                Debug.LogWarning($"[Effect] {source.Data.cardName}: No tenant in room, effect skipped.");
                return;
            }

            foreach (var tenant in room.GetTenants())
            {
                if (tenant.IsDestroyed) continue;
                tenant.DestroyEffect?.Execute(tenant, context);
                tenant.MarkDestroyed();
                Debug.Log($"[Effect] {source.Data.cardName}: Evicted tenant {tenant.Data.cardName} from room {room.RoomIndex}.");
                return;
            }
        }
    }
}
