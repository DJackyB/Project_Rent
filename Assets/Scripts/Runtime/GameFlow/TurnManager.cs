using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.Economy;
using BaoZuPo.Integration.Martian.Feedback;
using Martian.EventBus;
using Martian.RandomEvent;
using UnityEngine;
using BaoZuPo.UI;
using BaoZuPo.UI.Settlement;

namespace BaoZuPo.GameFlow
{
    /// <summary>
    /// 回合管理�?- 游戏三阶段流程的核心驱动
    ///
    /// 职责�?
    /// 1. 管理游戏回合状态机（Prepare -> Action -> Settle�?
    /// 2. 驱动三个阶段的执行逻辑（通过调用 Action_* 节点�?
    /// 3. 处理卡牌出牌的有效性检查与执行
    /// 4. 调度与事件总线的交互（发布阶段切换、卡牌效果、贷款、奖励等事件�?
    /// 5. 管理结算的播放流程与UI同步
    ///
    /// 流程概述�?
    /// - 准备阶段：卡牌前置效果触�?-> 抽牌 -> 清理过期卡牌
    /// - 行动阶段：等待玩家出卡和行动 -> 出卡费用结算与效果执�?
    /// - 结算阶段：房间租金收�?-> 合同效果 -> 耐久减少与驱�?-> 等待超时 -> 贷款支付 -> 奖励展示
    ///
    /// 状态机转移�?
    /// TurnStarted -> [Prepare] -> [Action] -> [Settle] -> TurnEnded -> (next turn)
    ///
    /// 事件序列�?
    /// - TurnStarted(TurnNumber)：回合开�?
    /// - PhaseChanged(Phase, PhaseName)：阶段切�?
    /// - CardPlayed(Card)：卡牌出牌完�?
    /// - CardDestroyed(Card, TriggeredByDurability)：卡牌销毁（耐久归零或等待超时）
    /// - LoanPayment(Amount, RemainingMoney)：贷款支�?
    /// - GameOver(FinalMoney, TotalTurns)：游戏结�?
    /// - CardRewardOffered(Options, Boosted)：奖励选项展示
    /// - CardRewardSelected(ChosenCard)：玩家选择奖励
    /// - SettlementPlaybackCompleted(BatchId)：结算动画全部播�?
    /// - TurnEnded(TurnNumber)：回合结�?
    /// </summary>
    public class TurnManager : Singleton<TurnManager>
    {
        private const string DefaultSettlementLaneKey = "settlement-global";
        private const float PreparePhaseLeadInSeconds = 0.12f;
        private const float PreparePhaseOutroSeconds = 0.08f;
        private const float RewardPickOutroSeconds = 0.1f;

        private ICardPlayService _cardPlayService = new CardPlayService();
        private ISettlementService _settlementService = new SettlementService();
        private ISettlementPresentationMapper _settlementPresentationMapper = new SettlementPresentationMapper();
        private ISettlementPresentationService _settlementPresentationService = new SettlementPresentationService();
        private IRewardService _rewardService = new RewardService();
        private IShopService _shopService = new ShopService();

        public void Construct(
            ICardPlayService cardPlayService,
            ISettlementService settlementService,
            ISettlementPresentationMapper settlementPresentationMapper,
            ISettlementPresentationService settlementPresentationService,
            IRewardService rewardService,
            IShopService shopService)
        {
            _cardPlayService = cardPlayService ?? throw new ArgumentNullException(nameof(cardPlayService));
            _settlementService = settlementService ?? throw new ArgumentNullException(nameof(settlementService));
            _settlementPresentationMapper = settlementPresentationMapper ?? throw new ArgumentNullException(nameof(settlementPresentationMapper));
            _settlementPresentationService = settlementPresentationService ?? throw new ArgumentNullException(nameof(settlementPresentationService));
            _rewardService = rewardService ?? throw new ArgumentNullException(nameof(rewardService));
            _shopService = shopService ?? throw new ArgumentNullException(nameof(shopService));
        }

        [Header("洗牌提示")]
        [SerializeField] private float shufflePauseBeforeSeconds = 0.35f;
        [SerializeField] private float shufflePauseAfterSeconds = 0.35f;
        [Header("����¼���")]
        [SerializeField] private Card.CardData _eventCardData;
        [SerializeField] [Range(0f, 1f)] private float _eventCardSpawnChance = 0.1f;

        [Header("Debug")]
        [SerializeField] private int _currentTurn;
        [SerializeField] private bool _isGameOver;
        [SerializeField] private int _loanPaymentCount;

        /// <summary>当前活跃的结算批�?ID（用于UI同步�?/summary>
        private string _activeSettlementBatchId;
        /// <summary>当前还有多少个结算播放任务未完成</summary>
        private int _pendingSettlementPlaybackCount;
        /// <summary>标记 TurnEnded 事件是否已发布（防止重复�?/summary>
        private bool _settlementTurnEndedPublished;
        /// <summary>标记是否等待玩家选择奖励�?/summary>
        private bool _isRewardFlowPending;
        /// <summary>标记是否等待结算后的随机事件流程完成。</summary>
        private bool _isPostSettlementRandomEventPending;
        private bool _isPreparePresentationPending;
        /// <summary>标记事件订阅是否已建�?/summary>
        private bool _eventsSubscribed;

        /// <summary>缓存当前回合�?boosted 奖励状态，供结算动画完成后使用（贷款周期到达时触发高稀有度池）</summary>
        private bool _pendingRewardBoosted;

        public int CurrentTurn => _currentTurn;
        public bool IsGameOver => _isGameOver;
        /// <summary>当前游戏阶段（Prepare、Action、Settle�?/summary>
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Prepare;
        /// <summary>行动阶段是否已结束（由玩家或 UI 设置�?/summary>
        public bool ActionPhaseEnded { get; set; }
        /// <summary>是否有结算播放任务待执行</summary>
        public bool IsSettlementPlaybackPending => _pendingSettlementPlaybackCount > 0;
        /// <summary>是否等待奖励流程完成（选择或动画）</summary>
        public bool IsRewardSelectionPending => _isRewardFlowPending;
        public bool IsPostSettlementRandomEventPending => _isPostSettlementRandomEventPending;
        public bool IsPreparePresentationPending => _isPreparePresentationPending;
        public bool IsShopOpen => _shopService.IsOpen;
        /// <summary>当前结算批次 ID（用于跟踪哪个结算完成）</summary>
        public string ActiveSettlementBatchId => _activeSettlementBatchId;

        public bool TryGetNextLoanPreview(out int dueTurn, out int amount)
        {
            dueTurn = 0;
            amount = 0;

            var gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.gameConfig == null)
            {
                return false;
            }

            var config = gameManager.gameConfig;
            if (config.loanInterval <= 0)
            {
                return false;
            }

            // 顶栏展示“下一次会发生的扣款”，当前回合若正好命中贷款周期，仍显示当前回合�?
            dueTurn = _currentTurn <= 0
                ? config.loanInterval
                : ((_currentTurn - 1) / config.loanInterval + 1) * config.loanInterval;
            amount = CalculateCurrentLoanPayment(config.loanAmount, config.loanGrowthFactor);
            return true;
        }

        private void OnEnable()
        {
            EnsureEventSubscriptions();
        }

        private void OnDisable()
        {
            if (!_eventsSubscribed)
            {
                return;
            }

            EventBus.Unsubscribe<GameEvents.SettlementPlaybackCompleted>(OnSettlementPlaybackCompleted);
            _eventsSubscribed = false;
        }

        /// <summary>
        /// 执行准备阶段
        ///
        /// 流程步骤�?
        /// 1. 回合计数器递增
        /// 2. 发布 TurnStarted 事件
        /// 3. 阶段切换�?Prepare，发�?PhaseChanged 事件
        /// 4. 执行所有场上卡牌的前置效果（PreEffect�?
        ///    - 包括租户、装备、合同的前置效果
        ///    - 按放置顺序执�?
        /// 5. 从抽卡库中抽�?
        ///    - 第一回合：按 firstTurnDrawCount �?firstTurnDrawLibrary 配置
        ///    - 后续回合：按 normalTurnDrawCount �?normalTurnDrawLibrary 配置
        /// 6. 清理场上已销毁的卡牌
        ///
        /// 外部系统交互�?
        /// - DeckManager：DrawFromLibrary（抽卡）
        /// - BoardManager：GetAllFieldCards、CleanupDestroyedCards
        /// - GameManager：gameConfig、GameContext
        /// - EventBus：TurnStarted、PhaseChanged
        /// </summary>
        public void ExecutePreparePhase()
        {
            EnsureGameManagerInitialized();

            if (_isGameOver)
            {
                return;
            }

            ResetShopStateForNewTurn();
            _currentTurn++;
            EventBus.Publish(new GameEvents.TurnStarted { TurnNumber = _currentTurn });
            PublishPhaseChanged(GamePhase.Prepare);

            var fieldCards = BoardManager.Instance.GetAllFieldCards();
            foreach (var card in fieldCards)
            {
                if (card == null || card.IsDestroyed)
                {
                    continue;
                }

                card.PreEffect?.Execute(card, GameManager.Instance.GameContext);
            }

            BoardManager.Instance.CleanupDestroyedCards();

            var config = GameManager.Instance.gameConfig;
            int drawCount = _currentTurn == 1 ? config.firstTurnDrawCount : config.normalTurnDrawCount;
            BeginPrepareDrawPresentation(drawCount);
        }

        private void BeginPrepareDrawPresentation(int drawCount)
        {
            _isPreparePresentationPending = false;
            bool useFirstTurnLibrary = _currentTurn == 1;
            var firstTurnLibrary = GameManager.Instance != null ? GameManager.Instance.gameConfig.firstTurnDrawLibrary : null;

            if (drawCount <= 0)
            {
                TrySpawnEventCard();
                UIManager.Instance?.RefreshAll();
                return;
            }

            if (!isActiveAndEnabled || UIManager.Instance == null || UIManager.Instance.handPanel == null)
            {
                if (useFirstTurnLibrary)
                {
                    Deck.DeckManager.Instance.DrawFromLibrary(firstTurnLibrary, drawCount);
                }
                else
                {
                    Deck.DeckManager.Instance.Draw(drawCount);
                }

                TrySpawnEventCard();
                UIManager.Instance?.RefreshAll();
                return;
            }

            _isPreparePresentationPending = true;
            StartCoroutine(PlayPrepareDrawSequence(
                useFirstTurnLibrary,
                firstTurnLibrary,
                drawCount,
                _currentTurn == 1 ? UIHandIncomingAnimationKind.FirstTurnDraw : UIHandIncomingAnimationKind.TurnDraw));
        }

        private IEnumerator PlayPrepareDrawSequence(
            bool useFirstTurnLibrary,
            CardLibrary firstTurnLibrary,
            int drawCount,
            UIHandIncomingAnimationKind animationKind)
        {
            if (UIManager.Instance == null || UIManager.Instance.handPanel == null)
            {
                if (useFirstTurnLibrary)
                {
                    Deck.DeckManager.Instance.DrawFromLibrary(firstTurnLibrary, drawCount);
                }
                else
                {
                    Deck.DeckManager.Instance.Draw(drawCount);
                }

                TrySpawnEventCard();
                _isPreparePresentationPending = false;
                yield break;
            }

            yield return new WaitForSeconds(PreparePhaseLeadInSeconds);

            for (int i = 0; i < drawCount; i++)
            {
                // 抽前检查：牌堆耗尽且有弃牌可循环，先停顿再洗牌
                if (!useFirstTurnLibrary
                    && Deck.DeckManager.Instance.DrawPileCount == 0
                    && Deck.DeckManager.Instance.DiscardPileCount > 0)
                {
                    yield return new WaitForSeconds(shufflePauseBeforeSeconds);
                    ShowShufflePopup();
                    Deck.DeckManager.Instance.ShuffleDiscardIntoDraw();
                    yield return new WaitForSeconds(shufflePauseAfterSeconds);
                }

                var drawn = useFirstTurnLibrary
                    ? Deck.DeckManager.Instance.DrawFromLibrary(firstTurnLibrary, 1)
                    : Deck.DeckManager.Instance.Draw(1);
                if (drawn == null || drawn.Count == 0)
                {
                    break;
                }

                yield return UIManager.Instance.handPanel.PlayIncomingCard(drawn[0], animationKind);
            }

            TrySpawnEventCard();

            if (drawCount > 0 && PreparePhaseOutroSeconds > 0f)
            {
                yield return new WaitForSeconds(PreparePhaseOutroSeconds);
            }

            _isPreparePresentationPending = false;
        }


        /// <summary>
        /// 启动行动阶段
        ///
        /// 职责�?
        /// 1. 重置 ActionPhaseEnded 标记（允许玩家再次出牌）
        /// 2. 阶段切换�?Action，发�?PhaseChanged 事件
        /// 3. 此后玩家可调�?PlayCard 方法出牌直到 EndActionPhase 被调�?
        /// </summary>
        public void StartActionPhase()
        {
            EnsureGameManagerInitialized();

            if (_isGameOver)
            {
                return;
            }

            ActionPhaseEnded = false;
            PublishPhaseChanged(GamePhase.Action);
        }

        /// <summary>
        /// 获取卡牌所需的目标类型（PlayArea �?Room�?
        /// </summary>
        public CardPlayTargetKind GetRequiredTargetKind(CardInstance card)
        {
            return CardTargeting.GetRequiredTargetKind(card != null ? card.Data : null);
        }

        /// <summary>
        /// 验证卡牌出牌是否有效
        ///
        /// 检查项（按优先级）�?
        /// 1. 游戏是否已结�?
        /// 2. 卡牌是否有效（非null、未销毁）
        /// 3. 当前是否在行动阶段且未结�?
        /// 4. 玩家金钱是否足以支付卡牌费用
        /// 5. 如果需要房间目标：
        ///    - 是否提供了目标房�?
        ///    - 目标房间是否有空位（租户、装备）
        ///
        /// 返回值：
        /// - IsValid: 是否通过验证
        /// - RequiredTargetKind: 目标类型（用�?UI 提示�?
        /// - BlockReason: 失败原因（用于日�?反馈�?
        /// - TargetRoom: 有效的目标房间（通过时返回）
        /// </summary>
        public CardPlayValidationResult ValidatePlay(CardInstance card, RoomSlot targetRoom = null)
        {
            return _cardPlayService.ValidatePlay(card, targetRoom);
        }

        /// <summary>
        /// 执行卡牌出牌逻辑
        ///
        /// 流程步骤�?
        /// 1. 验证卡牌是否可以出牌（ValidatePlay�?
        /// 2. 设置效果执行上下文的目标房间
        /// 3. 根据卡牌类型放置�?
        ///    - 合同卡：添加�?BoardManager.Contracts
        ///    - 房间卡（租户、装备）：放入目标房�?
        ///    - 即发卡：无需放置
        /// 4. 转调 CardPlayService 完成结算
        ///    - 费用扣除
        ///    - 即发效果执行
        ///    - 从手牌移�?
        ///    - 发布 CardPlayed 事件
        ///    - 发送数据反�?
        ///
        /// 返回值：true 出牌成功，false 验证失败或放置失败
        /// </summary>
        public bool PlayCard(CardInstance card, RoomSlot targetRoom = null)
        {
            return _cardPlayService.Play(card, targetRoom).Succeeded;
        }

        public bool CardNeedsRoomTarget(CardInstance card)
        {
            return GetRequiredTargetKind(card) == CardPlayTargetKind.Room;
        }

        /// <summary>
        /// 结束行动阶段
        ///
        /// 职责�?
        /// 1. 设置 ActionPhaseEnded 标记�?true
        /// 2. 阻止后续 PlayCard 调用（通过 ValidatePlay 检查）
        /// 3. NodeCanvas �?Action_ActionPhase 节点会轮询此标记，并在为 true 时完成任�?
        /// </summary>
        public void EndActionPhase()
        {
            if (_isGameOver)
            {
                return;
            }

            ActionPhaseEnded = true;
        }

        /// <summary>
        /// 执行结算阶段 - 游戏经济循环的核�?
        ///
        /// 流程步骤（严格顺序）�?
        /// 1. 阶段切换�?Settle，发�?PhaseChanged 事件
        /// 2. 初始化结算批次（UUID）和播放跟踪
        /// 3. 【房间结算�? ProcessRoomSettlements�?
        ///    - 遍历所有房间，如果有租户才处理该房�?
        ///    - 租户处理顺序：遍�?tenants 列表
        ///    - 执行每个租户�?SettleEffect（产生租金、额外收益）
        ///    - 装备处理顺序：遍�?equipments 列表
        ///    - 执行每个装备�?SettleEffect
        ///    - 耐久减少：场上所有卡�?CurrentDurability--
        ///    - 耐久判定：CurrentDurability <= 0 时标记为销�?
        /// 4. 【合同结算�? ProcessContractSettlements�?
        ///    - 遍历所有合�?
        ///    - 执行合同�?SettleEffect
        ///    - 合同耐久减少与销毁逻辑同房间卡�?
        /// 5. 【卡牌销毁与清理�? DestroyAndCleanupCards�?
        ///    - 执行销毁卡牌的 DestroyEffect
        ///    - 发布 CardDestroyed 事件
        ///    - 清理手牌中的等待�?
        /// 6. 【贷款支付�? ProcessLoanPayment�?
        ///    - 检查贷款周期（loanInterval�?
        ///    - 如果当前回合 % loanInterval == 0，计算贷款（按指数增长）
        ///    - 尝试扣除金钱，失败则 GameOver
        /// 7. 【奖励状态缓存】：
        ///    - 判断是否触发 boosted 奖励池（同贷款周期）
        /// 8. 【提交结算批次�? FinalizeBatch�?
        ///    - 统计最终金钱变�?
        ///    - 创建 UI 播放队列或直接进入奖�?完成
        ///
        /// 关键特性：
        /// - 房间遍历顺序：BoardManager.GetAllRooms() 返回的顺�?
        /// - 租户/装备遍历顺序：room.GetTenants() / room.GetEquipments() 返回的顺�?
        /// - 耐久减少只在有租户的房间进行
        /// - waitTurns 只由手牌等待逻辑处理，不作用于场上卡�?
        /// - 结算动画�?UI 通过 batchId 同步完成状�?
        ///
        /// 外部系统交互�?
        /// - BoardManager：GetAllRooms、GetAllContracts、GetAllFieldCards、CleanupDestroyedCards
        /// - MoneyManager：CurrentMoney、AddMoney、ReduceMoney
        /// - DeckManager：ResolveHandWaitAndDiscardExpired
        /// - UIManager：SubmitSettlementBatch
        /// - EventBus：PhaseChanged、LoanPayment、GameOver
        /// </summary>
        public void ExecuteSettlePhase()
        {
            EnsureGameManagerInitialized();

            if (_isGameOver)
            {
                return;
            }

            CloseCurrentShop();
            CleanupTemporaryHandCards();
            PublishPhaseChanged(GamePhase.Settle);

            var request = new SettlementRequest(_currentTurn, _loanPaymentCount, PlayHandWaitDiscardAnimations);
            var result = _settlementService.ResolveAsync(request).GetAwaiter().GetResult();
            _loanPaymentCount = result.NewLoanPaymentCount;
            _isGameOver = result.IsGameOver;
            _pendingRewardBoosted = result.RewardBoosted;
            UIManager.Instance?.handPanel?.RefreshHand();

            var settlementBatch = _settlementPresentationMapper.Map(result);
            FinalizeBatch(result, settlementBatch);
        }

        /// <summary>
        /// 处理房间结算 - 租金收入与耐久减少的核心流�?
        ///
        /// [伪代码流程]�?
        /// foreach room in GetAllRooms():
        ///     if room.TenantCount <= 0: continue  // 无租户则跳过该房�?
        ///
        ///     // 为每个租户和装备执行 SettleEffect，记录金钱变�?
        ///     AppendRoomSettlementStage(room)
        ///
        ///     // 耐久减少与驱逐判定（仅在该房间有租户时执行）
        ///     foreach card in room.GetAllCards():
        ///         if card.durability <= 0: continue  // 配置�?0 表示无耐久
        ///         card.CurrentDurability--
        ///         if card.CurrentDurability <= 0: mark for destroy
        ///
        /// 关键细节�?
        /// - 只在 TenantCount > 0 的房间进行耐久减少（优化：无住户的房间卡牌无意义）
        /// - 租户与装备都会进行耐久减少
        /// - 耐久归零判定：card.CurrentDurability <= 0（包括严格等�?0 的预防）
        /// - 销毁列表后续由 DestroyAndCleanupCards 统一处理
        ///
        /// 外部系统�?
        /// - BoardManager：GetAllRooms、room.GetAllCards、room.TenantCount
        /// - AppendRoomSettlementStage：详见对应方�?
        /// </summary>
        private void ProcessRoomSettlements(
            UISettlementPlaybackBatch batch,
            string batchId,
            GameContext sharedContext,
            ref int sourceIndex,
            List<CardInstance> toDestroy)
        {
            var rooms = BoardManager.Instance.GetAllRooms();
            foreach (var room in rooms)
            {
                if (room.TenantCount <= 0)
                {
                    continue;
                }

                AppendRoomSettlementStage(batch, batchId, room, sharedContext, ref sourceIndex);

                foreach (var card in room.GetAllCards())
                {
                    if (card == null || card.IsDestroyed || card.Data.durability <= 0)
                    {
                        continue;
                    }

                    card.CurrentDurability--;
                    if (card.CurrentDurability <= 0)
                    {
                        toDestroy.Add(card);
                    }
                }
            }
        }

        /// <summary>
        /// 处理合同结算
        ///
        /// [伪代码流程]�?
        /// foreach contract in GetAllContracts():
        ///     if contract.IsDestroyed: continue
        ///
        ///     // 执行合同的结算效果（如额外收益、扣款等�?
        ///     contractContext = CreateSettlementExecutionContext(sharedContext, null)
        ///     contract.SettleEffect?.Execute(contract, contractContext)
        ///
        ///     // 记录金钱变化，为 UI 生成结算数据
        ///     payload = CreateSettlementPayload(...)
        ///     batch.Stages.Add(UISettlementPlaybackStage.CreateSerial(...))
        ///
        ///     // 耐久减少与驱�?
        ///     if contract.durability > 0:
        ///         contract.CurrentDurability--
        ///         if contract.CurrentDurability <= 0: mark for destroy
        ///
        /// 关键细节�?
        /// - 合同�?SettleEffect 可能修改金钱（单向地体现在结�?UI 中）
        /// - 合同结算是串联的（CreateSerial），不像房间租户和装备的并联
        /// - 合同没有目标房间，contractContext.SelectedRoom �?null
        /// - 合同耐久减少逻辑与房间卡牌相�?
        ///
        /// 外部系统�?
        /// - BoardManager：GetAllContracts
        /// - CreateSettlementExecutionContext：创建执行上下文
        /// - CreateSettlementPayload：生�?UI 播放数据
        /// </summary>
        private void ProcessContractSettlements(
            UISettlementPlaybackBatch batch,
            string batchId,
            GameContext sharedContext,
            ref int sourceIndex,
            List<CardInstance> toDestroy)
        {
            var contracts = BoardManager.Instance.GetAllContracts();
            foreach (var contract in contracts)
            {
                if (contract == null || contract.IsDestroyed)
                {
                    continue;
                }

                int sourceStartMoney = MoneyManager.Instance.CurrentMoney;
                var contractContext = CreateSettlementExecutionContext(sharedContext, null);
                contractContext.SettlementCapture.Begin();
                contractContext.SettlementCapture.SourceCard = contract;
                contract.SettleEffect?.Execute(contract, contractContext);

                var payload = CreateSettlementPayload(
                    batchId,
                    ref sourceIndex,
                    GameEvents.SettlementSourceKind.Contract,
                    null,
                    contract,
                    contractContext.SettlementCapture,
                    sourceStartMoney);

                if (payload != null)
                {
                    payload.TrackIndex = 0;
                    payload.TrackCount = 1;
                    payload.LaneKey = BuildLaneKey(batchId, payload.SourceIndex);
                    batch.Stages.Add(UISettlementPlaybackStage.CreateSerial(
                        payload.Title,
                        UISettlementPlaybackEntry.Create(payload, payload.LaneKey)));
                }

                if (contract.Data.durability <= 0)
                {
                    continue;
                }

                contract.CurrentDurability--;
                if (contract.CurrentDurability <= 0)
                {
                    toDestroy.Add(contract);
                }
            }
        }

        /// <summary>
        /// 销毁卡牌并进行全局清理
        ///
        /// [伪代码流程]�?
        /// // 耐久减少导致的销毁（执行 DestroyEffect�?
        /// foreach card in toDestroy:
        ///     if card.IsDestroyed: continue
        ///     card.DestroyEffect?.Execute(card, sharedContext)
        ///     card.MarkDestroyed()
        ///     EventBus.Publish(CardDestroyed, TriggeredByDurability=true)
        /// // 全局清理
        /// BoardManager.CleanupDestroyedCards()  // 从房�?合同列表移除已销毁的卡牌
        /// DeckManager.ResolveHandWaitAndDiscardExpired()  // 处理手牌中的等待�?
        ///
        /// 关键细节�?
        /// - toDestroy：耐久归零的卡牌，执行 DestroyEffect（可能产生额外效果或触发�?
        /// - TriggeredByDurability 标记用于区分销毁原因（可供其他系统判断�?
        /// - CleanupDestroyedCards 从容器移除，并可能触发房间数据更�?
        /// - ResolveHandWaitAndDiscardExpired 只处理手牌中的等待卡�?
        ///
        /// 外部系统�?
        /// - BoardManager：CleanupDestroyedCards
        /// - DeckManager：ResolveHandWaitAndDiscardExpired
        /// - EventBus：CardDestroyed
        /// </summary>
        private void DestroyAndCleanupCards(
            List<CardInstance> toDestroy,
            GameContext sharedContext)
        {
            foreach (var card in toDestroy)
            {
                if (card == null || card.IsDestroyed)
                {
                    continue;
                }

                card.DestroyEffect?.Execute(card, sharedContext);
                card.MarkDestroyed();
                EventBus.Publish(new GameEvents.CardDestroyed { Card = card, TriggeredByDurability = true });
            }

            BoardManager.Instance.CleanupDestroyedCards();

            // 播放即将到期的手牌等待卡出场动画，再执行数据移除，最后刷新手牌布局
            var handPanel = UIManager.Instance?.handPanel;
            if (handPanel != null && Deck.DeckManager.Instance != null)
            {
                foreach (var card in Deck.DeckManager.Instance.Hand)
                {
                    if (card == null || card.Data.waitTurns <= 0) continue;
                    if (card.CurrentWait == 1)
                    {
                        handPanel.PlayDiscardAnimation(card);
                    }
                }
            }

            Deck.DeckManager.Instance.ResolveHandWaitAndDiscardExpired();

            handPanel?.RefreshHand();
        }

        /// <summary>
        /// 处理贷款支付 - 游戏失败判定的关键节�?
        ///
        /// [伪代码流程]�?
        /// if loanInterval <= 0 or CurrentTurn % loanInterval != 0:
        ///     return  // 不是贷款周期，跳�?
        ///
        /// requiredPayment = CalculateCurrentLoanPayment(baseAmount, growthFactor)
        ///     // 公式：baseAmount * Pow(growthFactor, loanPaymentCount)
        ///     // loanPaymentCount �?0 开始，每次成功支付递增
        ///     // 指数增长确保贷款不断升高难度
        ///
        /// paid = MoneyManager.ReduceMoney(requiredPayment)
        /// EventBus.Publish(LoanPayment)
        ///
        /// if not paid:
        ///     GameOver = true
        ///     EventBus.Publish(GameOver)
        ///     // 游戏结束，UI 显示失败界面
        /// else:
        ///     loanPaymentCount++
        ///     PublishLoanPayment(feedback)  // 数据反馈
        ///     // 继续游戏，下一个贷款周期时金额更高
        ///
        /// 关键细节�?
        /// - loanInterval 是贷款周期（每隔多少回合触发一次）
        /// - 贷款周期同时决定 boosted 奖励的触发时�?
        /// - 若贷款失败，立即结束游戏，不再进入奖励阶�?
        /// - loanPaymentCount 用于指数增长计算，成功支付时递增
        /// - 游戏失败是贷款扣款失败的唯一原因
        ///
        /// 外部系统�?
        /// - MoneyManager：ReduceMoney、CurrentMoney
        /// - EventBus：LoanPayment、GameOver
        /// - BaoZuPoFeedbackAdapter：PublishLoanPayment
        /// </summary>
        private void ProcessLoanPayment()
        {
            var config = GameManager.Instance.gameConfig;
            if (config.loanInterval <= 0 || _currentTurn % config.loanInterval != 0)
            {
                return;
            }

            int requiredPayment = CalculateCurrentLoanPayment(config.loanAmount, config.loanGrowthFactor);
            bool paid = MoneyManager.Instance.ReduceMoney(requiredPayment);
            EventBus.Publish(new GameEvents.LoanPayment
            {
                Amount = requiredPayment,
                RemainingMoney = MoneyManager.Instance.CurrentMoney
            });

            if (!paid)
            {
                _isGameOver = true;
                EventBus.Publish(new GameEvents.GameOver
                {
                    FinalMoney = MoneyManager.Instance.CurrentMoney,
                    TotalTurns = _currentTurn
                });
            }
            else
            {
                _loanPaymentCount++;
                BaoZuPoFeedbackAdapter.PublishLoanPayment(requiredPayment);
            }
        }

        private void FinalizeBatch(SettlementResult result, UISettlementPlaybackBatch settlementBatch)
        {
            if (result == null || settlementBatch == null || (settlementBatch.IsEmpty && settlementBatch.TotalDelta == 0))
            {
                // 无结算动画，直接进入奖励或完�?
                TryStartRewardOrComplete();
                return;
            }

            if (UIManager.Instance == null)
            {
                TryStartRewardOrComplete();
                return;
            }

            BeginSettlementPlayback(result.SettlementId, 1);
            _settlementPresentationService.PlayAsync(settlementBatch).GetAwaiter().GetResult();
        }

        private static void PlayHandWaitDiscardAnimations(IReadOnlyList<CardInstance> cards)
        {
            var handPanel = UIManager.Instance?.handPanel;
            if (handPanel == null || cards == null)
            {
                return;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null)
                {
                    handPanel.PlayDiscardAnimation(cards[i]);
                }
            }
        }

        public void NotifySettlementPlaybackCompleted(string batchId)
        {
            OnSettlementPlaybackCompleted(new GameEvents.SettlementPlaybackCompleted
            {
                BatchId = batchId
            });
        }

        private static void ShowShufflePopup()
        {
            var layer = UI.Common.FeedbackPopup.UIFeedbackPopupLayer.GetOrCreate(null);
            if (layer == null)
            {
                return;
            }

            var style = layer.DefaultStyle.Clone();
            style.HoldSeconds = 0.6f;

            layer.Show(new UI.Common.FeedbackPopup.UIFeedbackPopupRequest
            {
                Text = UI.GameText.DeckShuffled,
                Category = UI.Common.FeedbackPopup.UIFeedbackPopupCategory.Default,
                ScreenOffset = Vector2.zero,
                UseScreenCenterFallback = true,
                Style = style,
            });
        }

        private void ResetShopStateForNewTurn()
        {
            _shopService.Close(_currentTurn);
            _shopService.ResetForNewTurn();
            CleanupTemporaryHandCards();
        }

        private void CleanupTemporaryHandCards()
        {
            if (Deck.DeckManager.Instance == null || Deck.DeckManager.Instance.HandCount <= 0)
            {
                return;
            }

            var temporaryCards = Deck.DeckManager.Instance.Hand
                .Where(card => card != null && card.RemoveFromHandAtTurnEnd)
                .ToArray();

            for (int i = 0; i < temporaryCards.Length; i++)
            {
                Deck.DeckManager.Instance.RemoveFromHand(temporaryCards[i]);
                temporaryCards[i].MarkDestroyed();
            }
        }

        public void OpenShop(CardInstance source)
        {
            EnsureGameManagerInitialized();

            if (_isGameOver || CurrentPhase != GamePhase.Action || ActionPhaseEnded)
                return;

            _shopService.Open();
        }

        public bool TryPurchaseShopOffer(int offerIndex)
        {
            return _shopService.TryPurchase(offerIndex);
        }

        public void CloseCurrentShop()
        {
            _shopService.Close(_currentTurn);
        }

        /// <summary>
        /// 抽卡结束后，按概率向手牌插入一张事件卡（不占抽牌数）�?
        /// 事件�?waitTurns=1，若本回合未打出，结算阶段将自动销毁�?
        /// </summary>
        private void TrySpawnEventCard()
        {
            if (_eventCardData == null)
            {
                return;
            }

            if (UnityEngine.Random.value >= _eventCardSpawnChance)
            {
                return;
            }

            Deck.DeckManager.Instance.AddCardToHand(_eventCardData);
        }

        private void PublishPhaseChanged(GamePhase phase)
        {
            CurrentPhase = phase;
            EventBus.Publish(new GameEvents.PhaseChanged
            {
                Phase = phase,
                PhaseName = phase.ToString()
            });
        }

        private void AppendRoomSettlementStage(
            UISettlementPlaybackBatch batch,
            string batchId,
            RoomSlot room,
            GameContext sharedContext,
            ref int sourceIndex)
        {
            if (batch == null || room == null)
            {
                return;
            }

            var roomPayloads = new List<GameEvents.SettlementSequenceQueued>();

            var tenants = room.GetTenants();
            for (int i = 0; i < tenants.Count; i++)
            {
                var tenant = tenants[i];
                if (tenant == null || tenant.IsDestroyed)
                {
                    continue;
                }

                int sourceStartMoney = MoneyManager.Instance.CurrentMoney;
                var tenantContext = CreateSettlementExecutionContext(sharedContext, room);
                tenantContext.SettlementCapture.Begin();
                tenantContext.SettlementCapture.SourceCard = tenant;

                int baseRent = Mathf.Max(0, tenant.Data != null ? tenant.Data.baseRent : 0);
                if (baseRent > 0)
                {
                    MoneyManager.Instance.AddMoney(baseRent);
                    tenantContext.SettlementCapture.RecordBase(baseRent, GameText.SettlementBase);
                }

                tenant.SettleEffect?.Execute(tenant, tenantContext);

                var payload = CreateSettlementPayload(
                    batchId,
                    ref sourceIndex,
                    GameEvents.SettlementSourceKind.Room,
                    room,
                    tenant,
                    tenantContext.SettlementCapture,
                    sourceStartMoney);

                if (payload != null)
                {
                    roomPayloads.Add(payload);
                }
            }

            bool roomHasTidy = TagQuery.RoomHasTag(room, TagType.Tidy);

            var equipments = room.GetEquipments();
            for (int i = 0; i < equipments.Count; i++)
            {
                var equipment = equipments[i];
                if (equipment == null || equipment.IsDestroyed)
                {
                    continue;
                }

                int sourceStartMoney = MoneyManager.Instance.CurrentMoney;
                var equipmentContext = CreateSettlementExecutionContext(sharedContext, room);
                equipmentContext.SettlementCapture.Begin();
                equipmentContext.SettlementCapture.SourceCard = equipment;

                int moneyBeforeEquipment = MoneyManager.Instance.CurrentMoney;
                equipment.SettleEffect?.Execute(equipment, equipmentContext);

                // 整洁词条：房间有 Tidy 租客时装备收益翻倍
                if (roomHasTidy)
                {
                    int equipmentDelta = MoneyManager.Instance.CurrentMoney - moneyBeforeEquipment;
                    if (equipmentDelta > 0)
                    {
                        MoneyManager.Instance.AddMoney(equipmentDelta);
                        if (equipmentContext.SettlementCapture.IsCapturing)
                        {
                            equipmentContext.SettlementCapture.RecordDelta(equipmentDelta, GameText.TagTidy);
                        }
                    }
                }

                var payload = CreateSettlementPayload(
                    batchId,
                    ref sourceIndex,
                    GameEvents.SettlementSourceKind.Room,
                    room,
                    equipment,
                    equipmentContext.SettlementCapture,
                    sourceStartMoney);

                if (payload != null)
                {
                    roomPayloads.Add(payload);
                }
            }

            if (roomPayloads.Count == 0)
            {
                return;
            }

            var entries = new UISettlementPlaybackEntry[roomPayloads.Count];
            for (int i = 0; i < roomPayloads.Count; i++)
            {
                var payload = roomPayloads[i];
                payload.TrackIndex = 0;
                payload.TrackCount = 1;
                payload.LaneKey = BuildLaneKey(batchId, payload.SourceIndex);
                entries[i] = UISettlementPlaybackEntry.Create(payload, payload.LaneKey);
            }

            batch.Stages.Add(UISettlementPlaybackStage.CreateSerial(
                ResolveSettlementTitle(GameEvents.SettlementSourceKind.Room, room, null),
                entries));
        }

        private GameEvents.SettlementSequenceQueued CreateSettlementPayload(
            string batchId,
            ref int sourceIndex,
            GameEvents.SettlementSourceKind sourceKind,
            RoomSlot room,
            CardInstance card,
            SettlementCaptureContext capture,
            int sourceStartMoney)
        {
            if (capture == null)
            {
                return null;
            }

            int finalAmount = MoneyManager.Instance.CurrentMoney - sourceStartMoney;
            int capturedStepCount = capture.Steps.Count;
            var steps = capture.Complete(finalAmount, includeFinalStep: false);

            if (capturedStepCount == 0 && finalAmount == 0)
            {
                return null;
            }

            return new GameEvents.SettlementSequenceQueued
            {
                BatchId = batchId,
                SourceIndex = sourceIndex++,
                SourceCount = 0,
                LaneKey = DefaultSettlementLaneKey,
                SourceKind = sourceKind,
                Room = room,
                Card = card,
                Title = ResolveSettlementTitle(sourceKind, room, card),
                Steps = steps,
                FinalAmount = finalAmount,
                TrackIndex = 0,
                TrackCount = 1
            };
        }

        private void BeginSettlementPlayback(string batchId, int pendingCount)
        {
            _activeSettlementBatchId = batchId;
            _pendingSettlementPlaybackCount = Mathf.Max(0, pendingCount);
            _settlementTurnEndedPublished = false;
        }

        private void OnSettlementPlaybackCompleted(GameEvents.SettlementPlaybackCompleted e)
        {
            if (string.IsNullOrWhiteSpace(e.BatchId) || !string.Equals(e.BatchId, _activeSettlementBatchId, StringComparison.Ordinal))
            {
                return;
            }

            if (_pendingSettlementPlaybackCount > 0)
            {
                _pendingSettlementPlaybackCount--;
            }

            if (_pendingSettlementPlaybackCount <= 0)
            {
                // 结算动画全部播完，进入奖励选择或直接完�?
                TryStartRewardOrComplete();
            }
        }

        /// <summary>
        /// 结算动画播完后，决定是展示三选一奖励还是直接结束回合�?
        /// </summary>
        private void TryStartRewardOrComplete()
        {
            if (HasPendingExitAnimations())
            {
                _pendingSettlementPlaybackCount++;
                StartCoroutine(WaitForExitAnimationsThenReward());
                return;
            }

            DoStartRewardOrComplete();
        }

        private IEnumerator WaitForExitAnimationsThenReward()
        {
            while (HasPendingExitAnimations())
            {
                yield return null;
            }

            _pendingSettlementPlaybackCount--;
            DoStartRewardOrComplete();
        }

        private void DoStartRewardOrComplete()
        {
            if (!_isGameOver && TryStartPostSettlementRandomEvent())
            {
                return;
            }

            StartRewardSelectionOrComplete();
        }

        private void StartRewardSelectionOrComplete()
        {
            if (!_isGameOver)
            {
                _isRewardFlowPending = true;
                StartCoroutine(RunRewardFlowAsync(_pendingRewardBoosted));
                return;
            }
            CompleteSettlementPhase();
        }

        private IEnumerator RunRewardFlowAsync(bool boosted)
        {
            RewardChoiceResult choice = default;
            bool taskDone = false;

            AwaitRewardChoiceAsync(boosted, result => choice = result, () => taskDone = true).Forget();

            yield return new WaitUntil(() => taskDone);

            if (choice.ChosenCard != null)
            {
                var addedCard = Deck.DeckManager.Instance.AddCardToHand(choice.ChosenCard);
                if (isActiveAndEnabled && UIManager.Instance?.handPanel != null && addedCard != null)
                {
                    Vector3? sourcePosition = choice.HasSourceWorldPosition ? choice.SourceWorldPosition : (Vector3?)null;
                    yield return UIManager.Instance.handPanel.PlayIncomingCard(addedCard, UIHandIncomingAnimationKind.RewardPick, sourcePosition);
                }

                if (RewardPickOutroSeconds > 0f)
                    yield return new WaitForSeconds(RewardPickOutroSeconds);
            }

            _isRewardFlowPending = false;
            CompleteSettlementPhase();
        }

        private async UniTask AwaitRewardChoiceAsync(
            bool boosted,
            Action<RewardChoiceResult> onCompleted,
            Action onFinally)
        {
            try
            {
                var result = await _rewardService.OfferAndWaitChoiceAsync(boosted, destroyCancellationToken);
                onCompleted?.Invoke(result);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                onFinally?.Invoke();
            }
        }

        private bool TryStartPostSettlementRandomEvent()
        {
            var config = GameManager.Instance != null ? GameManager.Instance.gameConfig : null;
            if (config == null
                || config.postSettlementRandomEventChance <= 0f
                || string.IsNullOrWhiteSpace(config.postSettlementRandomEventLibraryId))
            {
                return false;
            }

            float chance = Mathf.Clamp01(config.postSettlementRandomEventChance);
            if (UnityEngine.Random.value >= chance)
            {
                return false;
            }

            if (!RandomEventDatabase.IsLoaded)
            {
                Debug.LogWarning("[TurnManager] Post-settlement random event skipped because RandomEventDatabase is not loaded.");
                return false;
            }

            if (!RandomEventDatabase.TryGetLibraryById(config.postSettlementRandomEventLibraryId, out var library)
                || library == null
                || library.entries == null
                || library.entries.Count == 0)
            {
                Debug.LogWarning($"[TurnManager] Post-settlement random event library '{config.postSettlementRandomEventLibraryId}' is not available.");
                return false;
            }

            var manager = RandomEventManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[TurnManager] Post-settlement random event requested but RandomEventManager is missing.");
                return false;
            }

            _isPostSettlementRandomEventPending = true;
            manager.TriggerRandomFromLibrary(
                config.postSettlementRandomEventLibraryId,
                _ =>
                {
                    if (this != null)
                    {
                        StartCoroutine(WaitForPostSettlementRandomEventThenReward());
                    }
                });

            if (!manager.IsEventActive)
            {
                _isPostSettlementRandomEventPending = false;
                return false;
            }

            return true;
        }

        private IEnumerator WaitForPostSettlementRandomEventThenReward()
        {
            yield return null;

            var manager = RandomEventManager.Instance;
            while (manager != null && manager.IsEventActive)
            {
                yield return null;
                manager = RandomEventManager.Instance;
            }

            _isPostSettlementRandomEventPending = false;
            StartRewardSelectionOrComplete();
        }

        private bool HasPendingExitAnimations()
        {
            var handPanel = UIManager.Instance?.handPanel;
            var boardPanel = UIManager.Instance?.boardPanel;
            return (handPanel != null && handPanel.HasExitingAnimations)
                || (boardPanel != null && boardPanel.HasDestroyAnimations);
        }

        private void CompleteSettlementPhase()
        {
            if (_settlementTurnEndedPublished || _isRewardFlowPending || _isPostSettlementRandomEventPending)
            {
                return;
            }

            _settlementTurnEndedPublished = true;
            _pendingSettlementPlaybackCount = 0;
            _activeSettlementBatchId = null;
            EventBus.Publish(new GameEvents.TurnEnded { TurnNumber = _currentTurn });
        }

        private static string ResolveSettlementTitle(GameEvents.SettlementSourceKind sourceKind, RoomSlot room, CardInstance card)
        {
            return sourceKind switch
            {
                GameEvents.SettlementSourceKind.Room when card != null => CardText.Name(card),
                GameEvents.SettlementSourceKind.Room when room != null => GameText.SettlementRoomTitle(room.RoomIndex + 1),
                GameEvents.SettlementSourceKind.Contract when card != null => CardText.Name(card),
                GameEvents.SettlementSourceKind.Event when card != null => CardText.Name(card),
                _ => GameText.SettlementFallbackTitle
            };
        }

        private static GameContext CreateSettlementExecutionContext(GameContext sharedContext, RoomSlot selectedRoom)
        {
            var context = new GameContext
            {
                MoneyManager = sharedContext != null ? sharedContext.MoneyManager : null,
                BoardManager = sharedContext != null ? sharedContext.BoardManager : null
            };

            context.EffectContext.SelectedRoom = selectedRoom;
            return context;
        }

        private static string BuildLaneKey(string batchId, int sourceIndex)
        {
            return $"{DefaultSettlementLaneKey}:{batchId}:{sourceIndex}";
        }

        private static void EnsureGameManagerInitialized()
        {
            GameManager.Instance?.EnsureInitialized();
        }

        private static void FinalizeSourceCounts(UISettlementPlaybackBatch batch)
        {
            if (batch == null || batch.Stages == null)
            {
                return;
            }

            int totalSourceCount = 0;
            for (int i = 0; i < batch.Stages.Count; i++)
            {
                var stage = batch.Stages[i];
                if (stage == null || stage.Entries == null)
                {
                    continue;
                }

                for (int j = 0; j < stage.Entries.Count; j++)
                {
                    if (stage.Entries[j] != null && stage.Entries[j].Payload != null)
                    {
                        totalSourceCount++;
                    }
                }
            }

            for (int i = 0; i < batch.Stages.Count; i++)
            {
                var stage = batch.Stages[i];
                if (stage == null || stage.Entries == null)
                {
                    continue;
                }

                for (int j = 0; j < stage.Entries.Count; j++)
                {
                    if (stage.Entries[j] != null && stage.Entries[j].Payload != null)
                    {
                        stage.Entries[j].Payload.SourceCount = totalSourceCount;
                    }
                }
            }
        }

        /// <summary>
        /// 计算当前贷款金额（指数增长）
        ///
        /// 公式�?
        /// requiredPayment = baseAmount * Pow(growthFactor, loanPaymentCount)
        ///
        /// 例子�?
        /// 第一次贷款：baseAmount * 1^0 = 100
        /// 第二次贷款：baseAmount * 1.2^1 = 120
        /// 第三次贷款：baseAmount * 1.2^2 = 144
        ///
        /// 参数验证�?
        /// - baseAmount < 0 时按 0 处理
        /// - growthFactor < 1 时按 1 处理（平坦曲线）
        /// - loanPaymentCount �?0 开始（每次成功支付递增�?
        /// </summary>
        private int CalculateCurrentLoanPayment(int baseAmount, float growthFactor)
        {
            int safeBase = Mathf.Max(0, baseAmount);
            float safeFactor = Mathf.Max(1f, growthFactor);
            float raw = safeBase * Mathf.Pow(safeFactor, _loanPaymentCount);
            return Mathf.RoundToInt(raw);
        }

        private void EnsureEventSubscriptions()
        {
            if (_eventsSubscribed)
            {
                return;
            }

            EventBus.Subscribe<GameEvents.SettlementPlaybackCompleted>(OnSettlementPlaybackCompleted);
            _eventsSubscribed = true;
        }
    }
}
