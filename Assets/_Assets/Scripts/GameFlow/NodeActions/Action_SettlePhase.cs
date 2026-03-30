using BaoZuPo.GameFlow;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace BaoZuPo.NodeActions
{
    [Category("BaoZuPo/Turn Flow")]
    [Name("Settle Phase")]
    [Description("Run settle phase: resolve effects, wait turns, and debt check.")]
    public class Action_SettlePhase : ActionTask
    {
        protected override void OnExecute()
        {
            TurnManager.Instance.ExecuteSettlePhase();
        }

        protected override void OnUpdate()
        {
            if (TurnManager.Instance == null
                || (!TurnManager.Instance.IsSettlementPlaybackPending && !TurnManager.Instance.IsRewardSelectionPending))
            {
                EndAction(true);
            }
        }
    }
}
