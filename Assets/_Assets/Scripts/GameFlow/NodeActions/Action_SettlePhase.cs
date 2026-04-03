using BaoZuPo.GameFlow;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace BaoZuPo.NodeActions
{
    /// <summary>
    /// 结算阶段节点任务
    ///
    /// NodeCanvas 中的轮询节点，管理结算的完整生命周期
    /// 包括：效果执行、UI 动画播放、奖励展示、贷款支付、游戏结束判定
    ///
    /// 职责：
    /// 1. 启动结算逻辑（OnExecute）
    /// 2. 等待结算完全结束（OnUpdate）
    /// 3. 处理异步结算播放与奖励选择
    ///
    /// 执行流程：
    /// - OnExecute：
    ///   * 调用 TurnManager.ExecuteSettlePhase()
    ///   * 发送 PhaseChanged(Settle) 事件
    ///   * 房间结算：遍历所有房间，执行租户和装备的 SettleEffect，产生租金
    ///   * 合同结算：执行合同的 SettleEffect
    ///   * 卡牌清理：销毁耐久归零的卡，移除等待超时的卡
    ///   * 贷款支付：按周期扣除贷款，失败时 GameOver
    ///   * 结算打包：创建 UISettlementPlaybackBatch 供 UI 动画播放
    ///   * 如果有动画：UIManager 接收 batch，开始播放结算序列
    ///   * 如果无动画且无奖励：直接进入 CompleteSettlementPhase
    ///
    /// - OnUpdate（每帧调用，直到完成）：
    ///   * 轮询两个标记：
    ///     - IsSettlementPlaybackPending：结算动画是否还在播放
    ///     - IsRewardSelectionPending：是否等待玩家选择奖励
    ///   * 当两者都为 false 时，回合正式结束
    ///     - UI 完成动画后发布 SettlementPlaybackCompleted 事件
    ///     - TurnManager.NotifySettlementPlaybackCompleted() 被调用
    ///     - 若需要奖励，AwardOneCardFromThreeOptions 发布 CardRewardOffered
    ///     - 玩家选择或跳过奖励后，IsRewardSelectionPending 恢复为 false
    ///     - CompleteSettlementPhase 发送 TurnEnded 事件
    ///     - 流程图进入下一回合准备阶段或游戏结束
    ///
    /// 异步流程图：
    /// ExecuteSettlePhase()
    ///     -> UIManager.SubmitSettlementBatch(batch)  // UI 开始播放
    ///     -> [每个结算项播放动画]
    ///     -> UI 发布 SettlementPlaybackCompleted(batchId)
    ///     -> TurnManager.OnSettlementPlaybackCompleted()
    ///     -> IsSettlementPlaybackPending = false
    ///     -> TryStartRewardOrComplete()
    ///         -> AwardOneCardFromThreeOptions()  // 如果需要奖励
    ///             -> EventBus.Publish(CardRewardOffered)
    ///             -> UI 显示三选一对话框
    ///             -> 玩家选择 -> CardRewardSelected 事件
    ///             -> TurnManager.OnCardRewardSelected()
    ///             -> IsRewardSelectionPending = false
    ///             -> CompleteSettlementPhase()
    ///         -> 或直接 CompleteSettlementPhase()  // 如果无奖励
    ///     -> TurnEnded 事件
    ///     -> OnUpdate 检测 !IsSettlementPlaybackPending && !IsRewardSelectionPending
    ///     -> EndAction(true)
    ///     -> 流程图继续（下一回合或游戏结束）
    ///
    /// 关键细节：
    /// - 结算可能产生 0 个或多个播放项（动画）
    /// - 如果所有金钱变化都在 ExecuteSettlePhase 中完成，但没有 UI 动画，则直接完成
    /// - 奖励池（高稀有度或常规）由贷款周期的 boosted 标记决定
    /// - 贷款失败直接 GameOver，不再展示奖励
    /// - TurnManager 通过 ActiveSettlementBatchId 与 UI 对应批次
    ///
    /// 外部系统交互：
    /// - TurnManager：ExecuteSettlePhase、IsSettlementPlaybackPending、IsRewardSelectionPending
    /// - UIManager：SubmitSettlementBatch
    /// - EventBus：SettlementPlaybackCompleted、CardRewardOffered、CardRewardSelected、TurnEnded
    /// </summary>
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
