using NodeCanvas.Framework;
using ParadoxNotion.Design;
using BaoZuPo.GameFlow;

namespace BaoZuPo.NodeActions
{
    [Category("包租婆/回合流程")]
    [Name("检查游戏结束")]
    [Description("检查回合系统是否已进入 GameOver 状态")]
    public class Condition_CheckGameOver : ConditionTask
    {
        protected override bool OnCheck()
        {
            // 以回合系统的终局标记为唯一标准，避免和金钱扣款逻辑不一致
            return TurnManager.Instance.IsGameOver;
        }
    }
}
