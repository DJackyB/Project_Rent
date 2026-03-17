using NodeCanvas.Framework;
using ParadoxNotion.Design;
using BaoZuPo.GameFlow;

namespace BaoZuPo.NodeActions
{
    [Category("包租婆/回合流程")]
    [Name("结算阶段")]
    [Description("执行结算阶段：结算效果、等待倒计时、还贷判定")]
    public class Action_SettlePhase : ActionTask
    {
        protected override void OnExecute()
        {
            TurnManager.Instance.ExecuteSettlePhase();
            EndAction(true);
        }
    }
}
