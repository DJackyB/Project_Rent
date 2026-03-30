using System;
using BaoZuPo.GameFlow;

namespace BaoZuPo.Save
{
    [Serializable]
    public sealed class TurnSaveState
    {
        public int currentTurn;
        public bool isGameOver;
        public int loanPaymentCount;
        public GamePhase currentPhase;
        public bool actionPhaseEnded;
    }
}
