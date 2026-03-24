using BaoZuPo.GameFlow;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace BaoZuPo.NodeActions
{
    [Category("BaoZuPo/Turn Flow")]
    [Name("Prepare Phase")]
    [Description("Run prepare phase: pre-settle effects, contracts, and draw step.")]
    public class Action_PreparePhase : ActionTask
    {
        protected override void OnExecute()
        {
            TurnManager.Instance.ExecutePreparePhase();
            EndAction(true);
        }
    }
}
