using System;
using System.Collections.Generic;
using BaoZuPo.Board;
using BaoZuPo.Core;
using BaoZuPo.Deck;
using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌效果执行的附加上下文。
    ///
    /// 在效果执行时提供额外的操作参数，如当前选中房间、结算捕获钩子等。
    /// 由 GameContext 的 EffectContext 持有，在整个游戏循环中保持一致。
    /// </summary>
    public class EffectExecutionContext
    {
        /// <summary>当前选中的房间目标，无则为 null。用于 AddMoneyBySelectedRoom 等依赖房间选择的效果。</summary>
        public RoomSlot SelectedRoom { get; set; }

        /// <summary>结算捕获钩子。在结算阶段使用，用于记录货币流的每一步（基础、增量、倍数、最终）。</summary>
        public SettlementCaptureContext SettlementCapture { get; set; }

        /// <summary>
        /// 防止 TriggerSelectedRoomSettle 效果进入递归。
        /// 当已在额外房间结算中时，此标志为真，防止嵌套的 TriggerSelectedRoomSettle 再次触发。
        /// </summary>
        public bool IsExtraRoomSettlementActive { get; set; }
    }

    /// <summary>
    /// 卡牌效果接口。
    ///
    /// 代表一个可执行的游戏效果。所有具体效果类都实现此接口，
    /// 允许通过 CardEffectFactory 统一地创建和执行。
    ///
    /// 实现规范：
    /// - 效果应该是原子操作，不应包含游戏逻辑外的副作用
    /// - 必须通过 GameContext 访问游戏状态（金钱、棋盘、卡组）
    /// - 错误应直接抛出异常或通过日志报告，不做静默 fallback
    /// - 可选：在结算阶段中记录步骤到 SettlementCaptureContext 用于 UI 显示
    /// </summary>
    public interface ICardEffect
    {
        /// <summary>
        /// 执行该效果。
        ///
        /// 参数说明：
        /// - source：触发该效果的卡牌实例。用于标识效果来源。
        /// - context：游戏上下文，包含所有必要的游戏状态管理器和执行参数。
        ///
        /// 访问游戏状态示例：
        /// - context.MoneyManager：金钱管理（AddMoney、ReduceMoney）
        /// - context.BoardManager：棋盘管理（放置卡牌、房间操作）
        /// - context.DeckManager：卡组管理（抽卡、卡牌池查询）
        /// - context.EffectContext.SelectedRoom：当前选中房间（如果有）
        /// - context.EffectContext.SettlementCapture：结算步骤记录（用于 UI 结算动画）
        /// </summary>
        void Execute(CardInstance source, GameContext context);
    }

    /// <summary>
    /// 共享的游戏上下文，在效果执行时使用。
    ///
    /// 集中存储所有游戏状态管理器的引用，确保效果执行时能够一致地访问和修改游戏状态。
    /// GameContext 由游戏循环创建并传入各个效果执行点。
    ///
    /// 包含的管理器：
    /// - MoneyManager：金钱系统，负责货币的增减
    /// - BoardManager：棋盘系统，管理房间和卡牌放置
    /// - DeckManager：卡组系统，管理抽牌和卡池
    /// - SettlementCapture：结算捕获，记录每一步金钱变化用于 UI 显示
    /// - EffectContext：效果执行上下文，包含房间选择等额外参数
    /// </summary>
    public class GameContext
    {
        /// <summary>金钱管理器。负责玩家金钱的增减和查询。</summary>
        public Economy.MoneyManager MoneyManager { get; set; }

        /// <summary>棋盘管理器。负责房间管理、卡牌放置、卡牌摧毁等。</summary>
        public Board.BoardManager BoardManager { get; set; }

        /// <summary>卡组管理器。负责抽牌、卡牌池查询、随机选择等。</summary>
        public DeckManager DeckManager { get; set; }

        /// <summary>结算捕获上下文。记录结算阶段的金钱步骤，用于 UI 展示。</summary>
        public SettlementCaptureContext SettlementCapture { get; }

        /// <summary>效果执行上下文。包含房间选择、额外结算保护等。</summary>
        public EffectExecutionContext EffectContext { get; }

        public GameContext()
        {
            SettlementCapture = new SettlementCaptureContext();
            EffectContext = new EffectExecutionContext
            {
                SettlementCapture = SettlementCapture
            };
        }
    }

    /// <summary>
    /// 结算捕获上下文。
    ///
    /// 在结算阶段，捕获并记录每一步金钱流的详细过程：
    /// 1. 基础租金（Base）
    /// 2. 增量修正（Delta：如装备加成、卡牌效果）
    /// 3. 倍数修正（Multiplier：如合同效果）
    /// 4. 最终金额（Final）
    ///
    /// 这些步骤用于生成结算动画和结算面板，让玩家清晰地看到收入来源。
    /// 注意：SettlementCapture 记录是可选的。如果不调用 Begin()，则不会捕获任何步骤。
    /// </summary>
    public sealed class SettlementCaptureContext
    {
        private readonly List<GameEvents.SettlementStep> _steps = new();

        /// <summary>是否正在捕获结算步骤。</summary>
        public bool IsCapturing { get; private set; }

        /// <summary>已捕获的结算步骤列表（只读）。</summary>
        public IReadOnlyList<GameEvents.SettlementStep> Steps => _steps;

        public CardInstance SourceCard { get; set; }

        /// <summary>开始捕获结算步骤。清空之前的步骤列表，设置捕获标志为真。</summary>
        public void Begin()
        {
            _steps.Clear();
            SourceCard = null;
            IsCapturing = true;
        }

        /// <summary>
        /// 记录基础金额步骤（租金基础）。
        ///
        /// 参数：
        /// - amount：金额数值
        /// - label：可选标签，用于 UI 显示（如"张三租金"）
        /// </summary>
        public void RecordBase(int amount, string label = null)
        {
            RecordStep(GameEvents.SettlementStepKind.Base, amount, false, label);
        }

        /// <summary>
        /// 记录增量步骤。用于记录装备加成、卡牌效果等增减。
        ///
        /// 参数：
        /// - amount：增减金额（可为负）
        /// - label：可选标签，用于 UI 显示（如"装备加成"）
        /// </summary>
        public void RecordDelta(int amount, string label = null)
        {
            RecordStep(GameEvents.SettlementStepKind.Delta, amount, false, label);
        }

        /// <summary>
        /// 记录倍数步骤。用于合同效果（如 150% 倍数）。
        ///
        /// 参数：
        /// - multiplier：倍数值（1.0 = 100%，1.5 = 150%）。内部转换为百分比存储（150）
        /// - label：可选标签，用于 UI 显示（如"合同加成"）
        /// </summary>
        public void RecordMultiplier(float multiplier, string label = null)
        {
            int amount = Mathf.RoundToInt(multiplier * 100f);
            RecordStep(GameEvents.SettlementStepKind.Multiplier, amount, true, label);
        }

        /// <summary>
        /// 完成捕获，返回所有结算步骤。
        ///
        /// 参数：
        /// - finalAmount：最终金额（结算后的实际值）
        /// - label：最终步骤的标签（可选）
        /// - includeFinalStep：是否添加 Final 步骤。如果为假，则直接返回中间步骤
        ///
        /// 返回：结算步骤数组，按顺序包含 Base、Delta、Multiplier、Final。
        /// </summary>
        public GameEvents.SettlementStep[] Complete(int finalAmount, string label = null, bool includeFinalStep = true)
        {
            if (includeFinalStep)
            {
                RecordStep(GameEvents.SettlementStepKind.Final, finalAmount, false, label);
            }

            IsCapturing = false;
            return _steps.ToArray();
        }

        /// <summary>重置捕获状态。清空步骤列表，设置捕获标志为假。</summary>
        public void Reset()
        {
            _steps.Clear();
            SourceCard = null;
            IsCapturing = false;
        }

        /// <summary>
        /// 内部方法，记录一个结算步骤。
        ///
        /// 只在捕获中时记录；0 金额的非 Final 步骤会被忽略。
        /// </summary>
        private void RecordStep(GameEvents.SettlementStepKind kind, int amount, bool isMultiplier, string label)
        {
            if (!IsCapturing)
            {
                return;
            }

            if (kind != GameEvents.SettlementStepKind.Final && amount == 0)
            {
                return;
            }

            _steps.Add(new GameEvents.SettlementStep
            {
                Kind = kind,
                Label = string.IsNullOrWhiteSpace(label) ? null : label,
                Amount = amount,
                IsMultiplier = isMultiplier,
                SourceCard = SourceCard
            });
        }
    }
}
