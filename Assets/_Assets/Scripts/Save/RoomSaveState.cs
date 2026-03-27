using System;
using System.Collections.Generic;
using BaoZuPo.Card;

namespace BaoZuPo.Save
{
    [Serializable]
    public sealed class RoomSaveState
    {
        public int roomIndex;
        public int tenantSlotCapacity;
        public int equipmentSlotCapacity;
        public List<CardRuntimeState> tenants = new();
        public List<CardRuntimeState> equipments = new();
    }
}
