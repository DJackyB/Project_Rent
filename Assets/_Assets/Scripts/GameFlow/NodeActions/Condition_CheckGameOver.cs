using NodeCanvas.Framework;
using ParadoxNotion.Design;
using BaoZuPo.GameFlow;
using BaoZuPo.Economy;

namespace BaoZuPo.NodeActions
{
    [Category("包租婆/回合流程")]
    [Name("检查游戏结束")]
    [Description("检查金钱是否为负数，决定游戏是否结束")]
    public class Condition_CheckGameOver : ConditionTask
    {
        protected override bool OnCheck()
        {
            // 返回 true 表示游戏结束（金钱为负）
            return MoneyManager.Instance.CurrentMoney < 0;
        }
    }
}
