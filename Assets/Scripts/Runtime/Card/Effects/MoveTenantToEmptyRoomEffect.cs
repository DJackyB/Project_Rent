using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 移动租客到空房间。
    /// </summary>
    public class MoveTenantToEmptyRoomEffect : ICardEffect
    {
        // 执行：从选中房间取出首个未销毁的租客，找到首个可容纳租客的空房间，将租客移动到该房间。
        public void Execute(CardInstance source, GameContext context)
        {
            var fromRoom = context.EffectContext.SelectedRoom;
            if (fromRoom == null || fromRoom.TenantCount <= 0)
            {
                Debug.LogWarning($"[Effect] {EffectSourceHelper.Name(source)}: no movable tenant in the selected room");
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

            if (targetTenant == null)
            {
                return;
            }

            foreach (var room in context.BoardManager.GetAllRooms())
            {
                if (room == fromRoom || !room.CanPlaceTenant)
                {
                    continue;
                }

                fromRoom.RemoveCard(targetTenant);
                room.PlaceCard(targetTenant);
                Debug.Log($"[Effect] {EffectSourceHelper.Name(source)}: moved tenant Room {fromRoom.RoomIndex} -> Room {room.RoomIndex}");
                return;
            }

            Debug.LogWarning($"[Effect] {EffectSourceHelper.Name(source)}: no empty room available for migration");
        }
    }
}
