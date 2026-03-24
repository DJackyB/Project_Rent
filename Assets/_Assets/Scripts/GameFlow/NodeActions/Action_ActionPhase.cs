using BaoZuPo.GameFlow;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace BaoZuPo.NodeActions
{
    [Category("BaoZuPo/Turn Flow")]
    [Name("Action Phase")]
    [Description("Wait for the player to end the action phase.")]
    public class Action_ActionPhase : ActionTask
    {
        protected override void OnExecute()
        {
            TurnManager.Instance.StartActionPhase();
        }

        protected override void OnUpdate()
        {
            if (TurnManager.Instance.ActionPhaseEnded)
            {
                EndAction(true);
            }
        }
    }
}
