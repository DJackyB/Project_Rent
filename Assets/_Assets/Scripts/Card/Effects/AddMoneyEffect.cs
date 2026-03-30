using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// Add a fixed amount of money.
    /// </summary>
    public class AddMoneyEffect : ICardEffect
    {
        private readonly int _amount;

        public AddMoneyEffect(int amount)
        {
            _amount = amount;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            context.MoneyManager.AddMoney(_amount);
            context?.SettlementCapture?.RecordDelta(_amount, source != null && source.Data != null ? source.Data.cardName : null);
            Debug.Log($"[镜像效果] {source.Data.cardName} 触发 AddMoney({_amount})");
        }
    }
}
