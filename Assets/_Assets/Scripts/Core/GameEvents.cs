using BaoZuPo.Board;
using BaoZuPo.GameFlow;

namespace BaoZuPo.Core
{
    /// <summary>
    /// 游戏和 UI 事件定义（发布-订阅）。
    /// 每个事件结构体都有对应的发布者和订阅者：
    ///   - 发布者：游戏逻辑系统（TurnManager、MoneyManager、BoardManager 等）
    ///   - 订阅者：UI 系统、音频系统、反馈系统、其他逻辑模块
    ///
    /// 事件通过 EventBus.Publish<T>(event) 触发，通过 EventBus.Subscribe<T>(callback) 订阅。
    /// </summary>
    public static class GameEvents
    {
        /// <summary>
        /// 结算步骤类型（反馈系统用于显示 step-by-step 收入变化）。
        /// - Base：基础租金（房间本身产生）
        /// - Delta：增量奖励（卡牌加成）
        /// - Multiplier：倍数加成（乘法）
        /// - Final：最终值
        /// </summary>
        public enum SettlementStepKind
        {
            Base,
            Delta,
            Multiplier,
            Final
        }

        /// <summary>
        /// 结算来源类型（用于定位反馈位置）。
        /// - Room：房间本身产生的租金
        /// - Contract：契约卡产生的收益
        /// - Event：事件卡产生的收益
        /// </summary>
        public enum SettlementSourceKind
        {
            Room,
            Contract,
            Event
        }

        /// <summary>
        /// 单一结算步骤（构成 SettlementSequenceQueued 中的 Steps 数组）。
        /// 反馈系统逐步展示这些步骤，形成 step-by-step 收入动画。
        /// </summary>
        public struct SettlementStep
        {
            public SettlementStepKind Kind;
            public string Label;            // 步骤标签（显示文本）
            public int Amount;              // 金额
            public bool IsMultiplier;       // 是否为倍数加成
        }

        /// <summary>
        /// 金钱变化事件（MoneyManager 发布）。
        /// 订阅者：反馈系统显示浮动数字，音频系统播放金币音效。
        /// 发布时机：任何导致金钱值变化的动作（消费、获得、支付贷款）
        /// </summary>
        public struct MoneyChanged
        {
            public int OldValue;
            public int NewValue;
            public int Delta;               // NewValue - OldValue
        }

        /// <summary>
        /// 金钱不足事件（MoneyManager 发布）。
        /// 订阅者：UI 系统显示警告，或阻止不合法操作。
        /// </summary>
        public struct MoneyInsufficient
        {
            public int CurrentMoney;
            public int RequiredAmount;
        }

        /// <summary>
        /// 回合开始事件（TurnManager 发布）。
        /// 订阅者：DeckManager 执行抽卡，音频播放回合开始音效。
        /// 发布时机：转法 FSM 进入 Action 阶段前
        /// </summary>
        public struct TurnStarted
        {
            public int TurnNumber;
        }

        /// <summary>
        /// 回合结束事件（TurnManager 发布）。
        /// 订阅者：棋盘结算逻辑、下一阶段的前置检查。
        /// </summary>
        public struct TurnEnded
        {
            public int TurnNumber;
        }

        /// <summary>
        /// 阶段变更事件（TurnManager 发布）。
        /// 游戏主要阶段：Prepare → Action → Settlement → Reward → NextTurn
        /// 订阅者：UI 系统更新阶段提示，音频播放阶段转换音效。
        /// </summary>
        public struct PhaseChanged
        {
            public GamePhase Phase;
            public string PhaseName;
        }

        /// <summary>
        /// 卡牌被打出事件（TurnManager 发布）。
        /// 订阅者：音频系统播放卡牌打出音效，反馈系统显示消费提示。
        /// 发布时机：卡牌进入棋盘或场景后
        /// </summary>
        public struct CardPlayed
        {
            public Card.CardInstance Card;
        }

        /// <summary>
        /// 卡牌被销毁事件（BoardManager/房间逻辑发布）。
        /// 订阅者：棋盘逻辑清理卡牌引用，反馈系统显示销毁提示。
        /// 发布时机：卡牌耐久度归零或被移除时
        /// </summary>
        public struct CardDestroyed
        {
            public Card.CardInstance Card;
            public bool TriggeredByDurability;  // true=耐久度到期，false=其他原因
        }

        /// <summary>
        /// 贷款支付事件（TurnManager 发布）。
        /// 订阅者：反馈系统显示贷款扣款，音频播放警告音效。
        /// 发布时机：每个贷款周期到来时（转数 % loanInterval == 0）
        /// </summary>
        public struct LoanPayment
        {
            public int Amount;              // 本次支付金额
            public int RemainingMoney;      // 支付后剩余金额
        }

        /// <summary>
        /// 游戏结束事件（TurnManager 发布）。
        /// 订阅者：GameManager 停止 FSM，UI 系统显示结果页面，音频播放结束音乐。
        /// 发布时机：无法支付贷款或达到游戏胜利条件时
        /// </summary>
        public struct GameOver
        {
            public int FinalMoney;          // 最终金钱
            public int TotalTurns;          // 玩到的总回合数
        }

        /// <summary>
        /// 结算序列入列事件（BoardManager 发布给反馈系统）。
        /// 一个 BatchId 可能包含多个 SettlementSequenceQueued（例如多间房间同时结算）。
        /// 反馈系统使用 LaneKey 和 TrackIndex/TrackCount 来并行管理多条结算序列。
        /// </summary>
        public sealed class SettlementSequenceQueued
        {
            public string BatchId;                      // 一批结算的唯一 ID
            public int SourceIndex;                     // 本序列在 SourceCount 中的索引
            public int SourceCount;                     // 本批总共有多少个源（房间/卡牌）
            public int TrackIndex;                      // 动画轨道索引（用于多序列并排显示）
            public int TrackCount = 1;                  // 动画轨道总数
            public string LaneKey;                      // 反馈系统的进度条键（用于控制并发）
            public SettlementSourceKind SourceKind;     // 来源类型（Room/Contract/Event）
            public RoomSlot Room;                       // 如果来自房间，这里是房间引用
            public Card.CardInstance Card;              // 如果来自卡牌，这里是卡牌引用
            public string Title;                        // 反馈系统显示的标题
            public SettlementStep[] Steps;              // Step-by-step 结算步骤数组
            public int FinalAmount;                     // 最终金额（Step.Amount 之和）
        }

        /// <summary>
        /// 结算序列播放完成事件（反馈系统发布）。
        /// 用于等待所有反馈动画完毕后才继续下一阶段。
        /// </summary>
        public struct SettlementPlaybackCompleted
        {
            public string BatchId;                      // 对应的 SettlementSequenceQueued.BatchId
        }

        /// <summary>
        /// 结算完成后展示三选一奖励事件（TurnManager 发布 → UI 订阅）。
        /// UI 显示三张卡牌给玩家选择，或跳过此奖励。
        /// 发布时机：结算序列全部完成后，奖励阶段开始时
        /// </summary>
        public struct CardRewardOffered
        {
            public Card.CardData[] Options;             // 三个选项的卡牌数据
            public bool Boosted;                        // 是否加成（例如倍数奖励）
        }

        /// <summary>
        /// 玩家选择奖励卡或跳过事件（UI 发布 → TurnManager 订阅）。
        /// UI 响应玩家点击后发布此事件，ChosenCard 为 null 表示跳过。
        /// </summary>
        public struct CardRewardSelected
        {
            public Card.CardData ChosenCard;            // 玩家选中的卡牌，null 表示跳过
        }
    }
}
