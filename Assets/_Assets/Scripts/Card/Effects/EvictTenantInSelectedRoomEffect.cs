using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 清退选中房间中的首个租客（触发销毁效果）
    /// 格式：EvictTenantInSelectedRoom
    /// </summary>
    public class EvictTenantInSelectedRoomEffect : ICardEffect
    {
        public void Execute(CardInstance source, GameContext context)
        {
            var room = context.EffectContext.SelectedRoom;
            if (room == null || room.TenantCount <= 0)
            {
                Debug.LogWarning($"[效果] {source.Data.cardName}: 房间无租客，效果跳过");
                return;
            }

            foreach (var tenant in room.GetTenants())
            {
                if (tenant.IsDestroyed) continue;
                tenant.DestroyEffect?.Execute(tenant, context);
                tenant.MarkDestroyed();
                Debug.Log($"[效果] {source.Data.cardName}: 清退租客 {tenant.Data.cardName}（房间{room.RoomIndex}）");
                return;
            }
        }
    }
}
