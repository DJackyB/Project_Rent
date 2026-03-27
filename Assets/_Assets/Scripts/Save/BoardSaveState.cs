using System;
using System.Collections.Generic;
using BaoZuPo.Card;

namespace BaoZuPo.Save
{
    [Serializable]
    public sealed class BoardSaveState
    {
        public List<RoomSaveState> rooms = new();
        public List<CardRuntimeState> contracts = new();
    }
}
