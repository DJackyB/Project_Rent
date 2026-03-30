using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// Reduce a fixed amount of money.
    /// </summary>
    public class ReduceMoneyEffect : ICardEffect
    {
        private readonly int _amount;

        public ReduceMoneyEffect(int amount)
        {
            _amount = amount;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            bool success = context.MoneyManager.ReduceMoney(_amount);
            if (success)
            {
                context?.SettlementCapture?.RecordDelta(-_amount, source != null && source.Data != null ? source.Data.cardName : null);
            }

            Debug.Log($"[镜像效果] {source.Data.cardName} 触发 ReduceMoney({_amount}), 成功: {success}");
        }
    }
}
