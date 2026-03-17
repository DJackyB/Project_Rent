namespace BaoZuPo.Core
{
    /// <summary>
    /// 游戏事件定义
    /// 配合 EventBus 使用：EventBus.Publish(new GameEvents.MoneyChanged { ... })
    /// </summary>
    public static class GameEvents
    {
        /// <summary>金钱变化事件</summary>
        public struct MoneyChanged
        {
            public int OldValue;
            public int NewValue;
            public int Delta; // 正数=增加，负数=减少
        }

        /// <summary>余额不足事件（尝试扣款但钱不够）</summary>
        public struct MoneyInsufficient
        {
            public int CurrentMoney;
            public int RequiredAmount;
        }

        /// <summary>回合开始事件</summary>
        public struct TurnStarted
        {
            public int TurnNumber;
        }

        /// <summary>回合结束事件</summary>
        public struct TurnEnded
        {
            public int TurnNumber;
        }

        /// <summary>阶段切换事件</summary>
        public struct PhaseChanged
        {
            public string PhaseName;
        }

        /// <summary>卡牌被打出事件</summary>
        public struct CardPlayed
        {
            public Card.CardInstance Card;
        }

        /// <summary>卡牌被销毁事件</summary>
        public struct CardDestroyed
        {
            public Card.CardInstance Card;
            public bool TriggeredByDurability; // true=耐久归零, false=等待归零
        }

        /// <summary>还贷事件</summary>
        public struct LoanPayment
        {
            public int Amount;
            public int RemainingMoney;
        }

        /// <summary>游戏结束事件</summary>
        public struct GameOver
        {
            public int FinalMoney;
            public int TotalTurns;
        }
    }
}
