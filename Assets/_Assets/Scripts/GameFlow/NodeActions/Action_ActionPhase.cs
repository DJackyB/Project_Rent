using BaoZuPo.GameFlow;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace BaoZuPo.NodeActions
{
    /// <summary>
    /// 行动阶段节点任务
    ///
    /// NodeCanvas 中的轮询节点，管理玩家的出卡行动与阶段结束
    ///
    /// 职责：
    /// 1. 初始化行动阶段（OnExecute）
    /// 2. 每帧轮询玩家是否结束行动（OnUpdate）
    /// 3. 等待完成信号后进入下一阶段
    ///
    /// 执行流程：
    /// - OnExecute：
    ///   * 调用 TurnManager.StartActionPhase()
    ///   * 重置 ActionPhaseEnded = false
    ///   * 发送 PhaseChanged(Action) 事件
    ///   * UI 接收事件，显示出卡界面
    ///
    /// - OnUpdate（每帧调用，直到 EndAction 为止）：
    ///   * 轮询 TurnManager.ActionPhaseEnded
    ///   * 当玩家点击"结束行动"按钮时，UI 调用 TurnManager.EndActionPhase()
    ///   * EndActionPhase 设置 ActionPhaseEnded = true
    ///   * 下一帧轮询时检测到 ActionPhaseEnded，调用 EndAction(true)
    ///   * 流程图进入结算阶段
    ///
    /// 关键细节：
    /// - ActionPhaseEnded 是 UI 与逻辑的通信信号
    /// - 所有出卡操作由 TurnManager.PlayCard() 处理，此节点不参与
    /// - TurnManager.ValidatePlay() 检查是否在行动阶段（CurrentPhase == Action && !ActionPhaseEnded）
    /// - 玩家无法在行动阶段结束后继续出卡
    ///
    /// 事件序列：
    /// PhaseChanged(Action) -> [玩家出卡] -> 结束行动 -> ActionPhaseEnded=true -> EndAction -> 进入结算
    /// </summary>
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
