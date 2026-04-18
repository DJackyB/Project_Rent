using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 直接增加指定金额。
    /// </summary>
    public class AddMoneyEffect : ICardEffect
    {
        private readonly int _amount;

        public AddMoneyEffect(int amount)
        {
            _amount = amount;
        }

        // 执行：直接将 _amount 增加到玩家金钱，并记录此次结算变化。
        public void Execute(CardInstance source, GameContext context)
        {
            context.MoneyManager.AddMoney(_amount);
            context?.SettlementCapture?.RecordDelta(_amount, source != null && source.Data != null ? source.Data.cardName : null);
            Debug.Log($"[MirrorEffect] {source.Data.cardName} triggered AddMoney({_amount})");
        }
    }
}
