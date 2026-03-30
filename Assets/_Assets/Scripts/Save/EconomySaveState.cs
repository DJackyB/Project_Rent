using System;

namespace BaoZuPo.Save
{
    [Serializable]
    public sealed class EconomySaveState
    {
        public int currentMoney;
        public int totalSpent;
    }
}
