using NodeCanvas.Framework;
using ParadoxNotion.Design;
using BaoZuPo.GameFlow;

namespace BaoZuPo.NodeActions
{
    [Category("包租婆/回合流程")]
    [Name("准备阶段")]
    [Description("执行准备阶段：回合+1、前置效果结算（含合同）、抽牌")]
    public class Action_PreparePhase : ActionTask
    {
        protected override void OnExecute()
        {
            TurnManager.Instance.ExecutePreparePhase();
            EndAction(true);
        }
    }
}
