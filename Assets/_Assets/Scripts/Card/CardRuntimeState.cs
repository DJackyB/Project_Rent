using System;

namespace BaoZuPo.Card
{
    [Serializable]
    public sealed class CardRuntimeState
    {
        public int cardId;
        public int currentDurability;
        public int currentWait;
    }
}
