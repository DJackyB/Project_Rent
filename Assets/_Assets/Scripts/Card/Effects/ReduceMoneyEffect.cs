using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 减少金钱效果
    /// 配置格式：ReduceMoney;100（减少100金钱）
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
            Debug.Log($"[效果] {source.Data.cardName} 触发 ReduceMoney({_amount}), 成功: {success}");
        }
    }
}
