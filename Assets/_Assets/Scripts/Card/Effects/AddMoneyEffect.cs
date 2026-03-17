using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 增加金钱效果
    /// 配置格式：AddMoney;100（增加100金钱）
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
            Debug.Log($"[效果] {source.Data.cardName} 触发 AddMoney({_amount})");
        }
    }
}
