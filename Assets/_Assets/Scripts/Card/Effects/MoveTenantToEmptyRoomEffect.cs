using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 将选中房间中的首个租客迁移到另一个有空位的房间
    /// 格式：MoveTenantToEmptyRoom
    /// </summary>
    public class MoveTenantToEmptyRoomEffect : ICardEffect
    {
        public void Execute(CardInstance source, GameContext context)
        {
            var fromRoom = context.EffectContext.SelectedRoom;
            if (fromRoom == null || fromRoom.TenantCount <= 0)
            {
                Debug.LogWarning($"[效果] {source.Data.cardName}: 目标房间无可迁移租客");
                return;
            }

            CardInstance targetTenant = null;
            foreach (var tenant in fromRoom.GetTenants())
            {
                if (!tenant.IsDestroyed)
                {
                    targetTenant = tenant;
                    break;
                }
            }

            if (targetTenant == null) return;

            foreach (var room in context.BoardManager.GetAllRooms())
            {
                if (room == fromRoom || !room.CanPlaceTenant) continue;
                fromRoom.RemoveCard(targetTenant);
                room.PlaceCard(targetTenant);
                Debug.Log($"[效果] {source.Data.cardName}: 租客迁移 房间{fromRoom.RoomIndex} -> 房间{room.RoomIndex}");
                return;
            }

            Debug.LogWarning($"[效果] {source.Data.cardName}: 没有可迁移到的空房间");
        }
    }
}
