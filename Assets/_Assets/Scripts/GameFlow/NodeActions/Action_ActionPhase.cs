using NodeCanvas.Framework;
using ParadoxNotion.Design;
using BaoZuPo.GameFlow;

namespace BaoZuPo.NodeActions
{
    [Category("包租婆/回合流程")]
    [Name("行动阶段")]
    [Description("等待玩家出牌，直到玩家手动结束行动阶段")]
    public class Action_ActionPhase : ActionTask
    {
        protected override void OnExecute()
        {
            TurnManager.Instance.StartActionPhase();
        }

        protected override void OnUpdate()
        {
            // 每帧检测，当玩家点击"结束回合"后 ActionPhaseEnded 被设为 true
            if (TurnManager.Instance.ActionPhaseEnded)
            {
                EndAction(true);
            }
        }
    }
}
