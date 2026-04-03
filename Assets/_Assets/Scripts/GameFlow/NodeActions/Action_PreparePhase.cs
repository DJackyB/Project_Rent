using BaoZuPo.GameFlow;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace BaoZuPo.NodeActions
{
    /// <summary>
    /// 准备阶段节点任务
    ///
    /// NodeCanvas 中的执行节点，驱动游戏的准备阶段
    ///
    /// 职责：
    /// 1. 调用 TurnManager.ExecutePreparePhase()
    /// 2. 立即完成任务（不等待）
    ///
    /// 流程细节：
    /// - 回合计数 +1
    /// - 发送 TurnStarted 事件
    /// - 发送 PhaseChanged(Prepare) 事件
    /// - 执行所有场上卡牌的前置效果（PreEffect）
    ///   * 租户、装备、合同都会触发
    ///   * 按放置顺序执行
    /// - 从配置的抽卡库中抽取卡牌
    ///   * 第一回合：firstTurnDrawCount、firstTurnDrawLibrary
    ///   * 后续回合：normalTurnDrawCount、normalTurnDrawLibrary
    /// - 清理场上已销毁的卡牌
    ///
    /// NodeCanvas 执行流：
    /// 游戏流程图 -> 调用 Action_PreparePhase -> OnExecute -> 准备阶段完成 -> EndAction(true) -> 流程图继续
    /// </summary>
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
