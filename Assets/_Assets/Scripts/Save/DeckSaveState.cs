using System;
using System.Collections.Generic;
using BaoZuPo.Card;

namespace BaoZuPo.Save
{
    [Serializable]
    public sealed class DeckSaveState
    {
        public int maxHandSize;
        public List<CardRuntimeState> drawPile = new();
        public List<CardRuntimeState> hand = new();
        public List<CardRuntimeState> discardPile = new();
    }
}
