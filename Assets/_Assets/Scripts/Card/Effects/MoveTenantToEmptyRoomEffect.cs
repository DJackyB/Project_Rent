using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    public class MoveTenantToEmptyRoomEffect : ICardEffect
    {
        public void Execute(CardInstance source, GameContext context)
        {
            var fromRoom = context.EffectContext.SelectedRoom;
            if (fromRoom == null || fromRoom.TenantCount <= 0)
            {
                Debug.LogWarning($"[Effect] {source.Data.cardName}: no movable tenant in the selected room");
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
                Debug.Log($"[Effect] {source.Data.cardName}: moved tenant Room {fromRoom.RoomIndex} -> Room {room.RoomIndex}");
                return;
            }

            Debug.LogWarning($"[Effect] {source.Data.cardName}: no empty room available for migration");
        }
    }
}
