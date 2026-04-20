using System.Collections.Generic;
using BaoZuPo.Board;

namespace BaoZuPo.Card
{
    public static class TagQuery
    {
        public static bool HasTag(CardData data, TagType tag)
        {
            if (data == null || data.tags == null) return false;
            foreach (var t in data.tags)
            {
                if (t == tag) return true;
            }
            return false;
        }

        public static bool HasTag(CardInstance card, TagType tag)
        {
            return card != null && HasTag(card.Data, tag);
        }

        public static bool RoomHasTag(RoomSlot room, TagType tag)
        {
            if (room == null) return false;
            foreach (var tenant in room.GetTenants())
            {
                if (HasTag(tenant, tag)) return true;
            }
            return false;
        }

        public static int CountTagInRoom(RoomSlot room, TagType tag)
        {
            if (room == null) return 0;
            int count = 0;
            foreach (var tenant in room.GetTenants())
            {
                if (HasTag(tenant, tag)) count++;
            }
            return count;
        }

        public static int SumBaseRentForTag(RoomSlot room, TagType tag)
        {
            if (room == null) return 0;
            int sum = 0;
            foreach (var tenant in room.GetTenants())
            {
                if (tenant != null && !tenant.IsDestroyed && HasTag(tenant, tag))
                    sum += tenant.Data != null ? tenant.Data.baseRent : 0;
            }
            return sum;
        }

        public static int CountTagOnBoard(IReadOnlyList<RoomSlot> rooms, TagType tag)
        {
            if (rooms == null) return 0;
            int count = 0;
            foreach (var room in rooms)
                count += CountTagInRoom(room, tag);
            return count;
        }

        public static RoomSlot GetAdjacentRoom(RoomSlot room, int offset, IReadOnlyList<RoomSlot> rooms)
        {
            if (room == null || rooms == null) return null;
            int targetIndex = room.RoomIndex + offset;
            foreach (var r in rooms)
            {
                if (r != null && r.RoomIndex == targetIndex) return r;
            }
            return null;
        }

        public static bool AllAdjacentRoomsHaveTag(RoomSlot room, TagType tag, IReadOnlyList<RoomSlot> rooms)
        {
            if (room == null || rooms == null) return false;
            var left = GetAdjacentRoom(room, -1, rooms);
            var right = GetAdjacentRoom(room, 1, rooms);
            if (left == null && right == null) return false;
            if (left != null && !RoomHasTag(left, tag)) return false;
            if (right != null && !RoomHasTag(right, tag)) return false;
            return true;
        }
    }
}
