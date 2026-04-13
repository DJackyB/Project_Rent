using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.Economy;
using BaoZuPo.Integration.Martian.Feedback;
using Martian.EventBus;
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
        private bool _isRewardSelectionPending;
        private bool _isPreparePresentationPending;
        /// <summary>标记事件订阅是否已建�?/summary>
        private bool _eventsSubscribed;

        /// <summary>缓存当前回合�?boosted 奖励状态，供结算动画完成后使用（贷款周期到达时触发高稀有度池）</summary>
        private bool _pendingRewardBoosted;
        private CardData[] _shopOptions = Array.Empty<CardData>();
        private bool[] _shopPurchased = Array.Empty<bool>();
        private bool _shopOpenedThisTurn;
        private bool _shopClosedThisTurn;
        private bool _isShopOpen;

        public int CurrentTurn => _currentTurn;
        public bool IsGameOver => _isGameOver;
        /// <summary>当前游戏阶段（Prepare、Action、Settle�?/summary>
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Prepare;
        /// <summary>行动阶段是否已结束（由玩家或 UI 设置�?/summary>
        public bool ActionPhaseEnded { get; set; }
        /// <summary>是否有结算播放任务待执行</summary>
        public bool IsSettlementPlaybackPending => _pendingSettlementPlaybackCount > 0;
        /// <summary>是否等待玩家从奖励池选择</summary>
        public bool IsRewardSelectionPending => _isRewardSelectionPending;
        public bool IsPreparePresentationPending => _isPreparePresentationPending;
        public bool IsShopOpen => _isShopOpen;
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
            EventBus.Unsubscribe<GameEvents.CardRewardSelected>(OnCardRewardSelected);
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
                InjectTurnShopCard();
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
                InjectTurnShopCard();
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
                InjectTurnShopCard();
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

            var shopCard = InjectTurnShopCard();
            if (shopCard != null)
            {
                yield return UIManager.Instance.handPanel.PlayIncomingCard(shopCard, animationKind);
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
            CardPlayTargetKind requiredTargetKind = GetRequiredTargetKind(card);

            if (_isGameOver)
            {
                return CardPlayValidationResult.Failure(CardPlayBlockReason.GameOver, requiredTargetKind, targetRoom);
            }

            if (card == null || card.Data == null || card.IsDestroyed)
            {
                return CardPlayValidationResult.Failure(CardPlayBlockReason.InvalidTarget, requiredTargetKind, targetRoom);
            }

            if (CurrentPhase != GamePhase.Action || ActionPhaseEnded)
            {
                return CardPlayValidationResult.Failure(CardPlayBlockReason.NotActionPhase, requiredTargetKind, targetRoom);
            }

            if (!MoneyManager.Instance.CanAfford(card.Data.cost))
            {
                return CardPlayValidationResult.Failure(CardPlayBlockReason.InsufficientMoney, requiredTargetKind, targetRoom);
            }

            if (requiredTargetKind == CardPlayTargetKind.Room)
            {
                if (targetRoom == null)
                {
                    return CardPlayValidationResult.Failure(CardPlayBlockReason.MissingTarget, requiredTargetKind);
                }

                if (CardTargeting.PersistsInRoom(card.Data))
                {
                    if (card.Data.cardType == CardType.Tenant && !targetRoom.CanPlaceTenant)
                    {
                        return CardPlayValidationResult.Failure(CardPlayBlockReason.TargetFull, requiredTargetKind, targetRoom);
                    }

                    if (card.Data.cardType == CardType.Equipment && !targetRoom.CanPlaceEquipment)
                    {
                        return CardPlayValidationResult.Failure(CardPlayBlockReason.TargetFull, requiredTargetKind, targetRoom);
                    }
                }
            }

            return CardPlayValidationResult.Success(requiredTargetKind, targetRoom);
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
        /// 4. 调用 ResolveCardAfterPlay 完成结算�?
        ///    - 费用扣除
        ///    - 即发效果执行
        ///    - 从手牌移�?
        ///    - 发布 CardPlayed 事件
        ///    - 发送数据反�?
        ///
        /// 返回值：true 出牌成功，false 验证失败或放置失�?
        /// </summary>
        public bool PlayCard(CardInstance card, RoomSlot targetRoom = null)
        {
            EnsureGameManagerInitialized();

            var validation = ValidatePlay(card, targetRoom);
            if (!validation.IsValid)
            {
                return false;
            }

            targetRoom = validation.TargetRoom;
            var context = GameManager.Instance.GameContext;
            context.EffectContext.SelectedRoom = targetRoom;

            if (CardTargeting.PersistsAsContract(card.Data))
            {
                return ResolveCardAfterPlay(card, context, c => BoardManager.Instance.AddContract(c), targetRoom);
            }

            if (CardTargeting.PersistsInRoom(card.Data))
            {
                if (targetRoom == null || !targetRoom.PlaceCard(card))
                {
                    Debug.LogWarning($"[TurnManager] Failed to place {card}");
                    return false;
                }
            }

            return ResolveCardAfterPlay(card, context, null, targetRoom);
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

            var sharedContext = GameManager.Instance.GameContext;
            int phaseStartMoney = MoneyManager.Instance.CurrentMoney;
            string batchId = Guid.NewGuid().ToString("N");
            int sourceIndex = 0;
            var settlementBatch = new UISettlementPlaybackBatch
            {
                CompletionBatchId = batchId,
                DeferredMoneyStartValue = phaseStartMoney
            };

            var toDestroy = new List<CardInstance>();

            ProcessRoomSettlements(settlementBatch, batchId, sharedContext, ref sourceIndex, toDestroy);
            ProcessContractSettlements(settlementBatch, batchId, sharedContext, ref sourceIndex, toDestroy);
            DestroyAndCleanupCards(toDestroy, sharedContext);

            ProcessLoanPayment();

            // 缓存 boosted 状态，奖励在结算动画播完后再展�?
            var config = GameManager.Instance.gameConfig;
            _pendingRewardBoosted = config.loanInterval > 0 && _currentTurn % config.loanInterval == 0;

            FinalizeBatch(settlementBatch, batchId);
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
        private static void DestroyAndCleanupCards(
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
            Deck.DeckManager.Instance.ResolveHandWaitAndDiscardExpired();
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

        private void FinalizeBatch(UISettlementPlaybackBatch settlementBatch, string batchId)
        {
            settlementBatch.DeferredMoneyEndValue = MoneyManager.Instance.CurrentMoney;
            FinalizeSourceCounts(settlementBatch);

            if (settlementBatch.IsEmpty && settlementBatch.TotalDelta == 0)
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

            BeginSettlementPlayback(batchId, 1);
            UIManager.Instance.SubmitSettlementBatch(settlementBatch);
        }

        public void NotifySettlementPlaybackCompleted(string batchId)
        {
            OnSettlementPlaybackCompleted(new GameEvents.SettlementPlaybackCompleted
            {
                BatchId = batchId
            });
        }

        /// <summary>
        /// 出牌后的结算 - 费用、效果、事件的统一入口
        ///
        /// [伪代码流程]�?
        /// 1. 尝试扣除费用
        ///    paid = MoneyManager.ReduceMoney(card.cost)
        ///    if not paid:
        ///        如果卡牌已放入房间，撤回
        ///        return false
        ///
        /// 2. 记录扣费前金钱数
        ///
        /// 3. 执行即发效果
        ///    card.InstantEffect?.Execute(card, context)
        ///    计算金钱变化（用于反馈）
        ///
        /// 4. 执行额外操作
        ///    afterInstant?.Invoke(card)  // 如添加合同、其他处�?
        ///
        /// 5. 从手牌移除卡�?
        ///    DeckManager.RemoveFromHand(card)
        ///
        /// 6. 发布事件与反�?
        ///    EventBus.Publish(CardPlayed)
        ///    BaoZuPoFeedbackAdapter.PublishPlayCost(...)
        ///    PublishPlaySequence(...)
        ///
        /// 返回值：true 出牌成功，false 费用不足
        ///
        /// 关键细节�?
        /// - afterInstant 是添加合同或其他持久化操作的时机（在效果与移除手牌之间）
        /// - 费用扣除失败时需撤回已放入房间的卡牌
        /// - instantMoneyDelta 用于 UI 反馈动画（如金钱浮点数）
        /// - targetRoom 优先使用参数，次优使�?card.PlacedRoom（已放置的房间）
        ///
        /// 外部系统�?
        /// - MoneyManager：ReduceMoney、CurrentMoney
        /// - DeckManager：RemoveFromHand
        /// - EventBus：CardPlayed
        /// - BaoZuPoFeedbackAdapter：PublishPlayCost、PublishInstantMoneyDelta
        /// </summary>
        private bool ResolveCardAfterPlay(
            CardInstance card,
            GameContext context,
            Action<CardInstance> afterInstant,
            RoomSlot targetRoom)
        {
            if (!MoneyManager.Instance.ReduceMoney(card.Data.cost))
            {
                if (targetRoom != null && card != null && card.PlacedRoom == targetRoom)
                {
                    targetRoom.RemoveCard(card);
                }

                return false;
            }

            int moneyBeforeInstant = MoneyManager.Instance.CurrentMoney;
            card.InstantEffect?.Execute(card, context);
            int instantMoneyDelta = MoneyManager.Instance.CurrentMoney - moneyBeforeInstant;

            afterInstant?.Invoke(card);
            Deck.DeckManager.Instance.RemoveFromHand(card);

            if (card.RemoveFromGameAfterPlay)
            {
                card.MarkDestroyed();
            }
            else if (!CardTargeting.PersistsInRoom(card.Data) && !CardTargeting.PersistsAsContract(card.Data))
            {

            // 即发卡（不上场的卡）打出后进弃卡池参与循环，并播放出场动�?
                Deck.DeckManager.Instance.SendToDiscard(card);
                UIManager.Instance?.handPanel?.PlayDiscardAnimation(card);
            }

            EventBus.Publish(new GameEvents.CardPlayed { Card = card });
            BaoZuPoFeedbackAdapter.PublishPlayCost(card, targetRoom ?? card.PlacedRoom, card.Data.cost);
            PublishPlaySequence(card, targetRoom ?? card.PlacedRoom, instantMoneyDelta);
            return true;
        }

        private void PublishPlaySequence(CardInstance card, RoomSlot targetRoom, int moneyDelta)
        {
            if (card == null || moneyDelta == 0)
            {
                return;
            }

            BaoZuPoFeedbackAdapter.PublishInstantMoneyDelta(card, targetRoom, moneyDelta);
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
            CloseCurrentShop();
            CleanupTemporaryHandCards();

            _shopOptions = Array.Empty<CardData>();
            _shopPurchased = Array.Empty<bool>();
            _shopOpenedThisTurn = false;
            _shopClosedThisTurn = false;
            _isShopOpen = false;
        }

        private CardInstance InjectTurnShopCard()
        {
            var config = GameManager.Instance != null ? GameManager.Instance.gameConfig : null;
            if (config == null || config.shopCard == null)
            {
                return null;
            }

            CleanupTemporaryHandCards();
            return Deck.DeckManager.Instance.ForceAddCardToHand(config.shopCard, card => card.ConfigureAsTemporaryHandCard());
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
            {
                return;
            }

            if (_shopOpenedThisTurn || _shopClosedThisTurn || _isShopOpen)
            {
                return;
            }

            var config = GameManager.Instance != null ? GameManager.Instance.gameConfig : null;
            if (config == null || config.shopLibrary == null)
            {
                throw new InvalidOperationException("[TurnManager] Shop opening requires GameConfig.shopLibrary.");
            }

            if (UIManager.Instance == null)
            {
                throw new InvalidOperationException("[TurnManager] Shop opening requires UIManager in scene.");
            }

            var sourceCards = config.shopLibrary.entries != null
                ? config.shopLibrary.entries.Select(entry => entry != null ? entry.card : null).Where(card => card != null).ToList()
                : new List<CardData>();
            if (sourceCards.Count == 0)
            {
                Debug.LogWarning("[TurnManager] No shop cards available.");
                return;
            }

            int desiredCount = Mathf.Clamp(config.shopOfferCount, 1, GameConfig.MaxShopOfferCount);
            _shopOptions = BuildUniqueCardOptions(sourceCards, desiredCount);
            if (_shopOptions.Length == 0)
            {
                Debug.LogWarning("[TurnManager] No unique shop cards available.");
                return;
            }

            if (_shopOptions.Length < desiredCount)
            {
                Debug.LogWarning($"[TurnManager] Shop library only has {_shopOptions.Length} unique shop card(s) available. Offering {_shopOptions.Length} unique option(s).");
            }

            _shopPurchased = new bool[_shopOptions.Length];
            _shopOpenedThisTurn = true;
            _isShopOpen = true;
            EventBus.Publish(new GameEvents.ShopOpened
            {
                Options = _shopOptions
            });
        }

        public bool TryPurchaseShopOffer(int offerIndex)
        {
            if (!_isShopOpen || offerIndex < 0 || offerIndex >= _shopOptions.Length)
            {
                return false;
            }

            if (_shopPurchased[offerIndex])
            {
                return false;
            }

            var chosen = _shopOptions[offerIndex];
            if (chosen == null)
            {
                return false;
            }

            if (chosen.cost > 0 && !MoneyManager.Instance.ReduceMoney(chosen.cost))
            {
                return false;
            }

            Deck.DeckManager.Instance.AddCardToDrawPile(chosen);
            _shopPurchased[offerIndex] = true;
            return true;
        }

        public void CloseCurrentShop()
        {
            if (!_isShopOpen)
            {
                return;
            }

            _isShopOpen = false;
            _shopClosedThisTurn = true;
            EventBus.Publish(new GameEvents.ShopClosed
            {
                TurnNumber = _currentTurn
            });
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
                equipment.SettleEffect?.Execute(equipment, equipmentContext);

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
            if (!_isGameOver)
            {
                AwardOneCardFromThreeOptions(_pendingRewardBoosted);
                // 如果 AwardOneCardFromThreeOptions 设置�?_isRewardSelectionPending�?
                // 则等待玩家选择；否则（无可用卡）直接完�?
                if (_isRewardSelectionPending)
                {
                    return;
                }
            }

            CompleteSettlementPhase();
        }

        private void OnCardRewardSelected(GameEvents.CardRewardSelected e)
        {
            if (!_isRewardSelectionPending)
            {
                return;
            }

            if (e.ChosenCard == null)
            {
                _isRewardSelectionPending = false;
                CompleteSettlementPhase();
                return;
            }

            if (!isActiveAndEnabled || UIManager.Instance == null || UIManager.Instance.handPanel == null)
            {
                Deck.DeckManager.Instance.AddCardToHand(e.ChosenCard);
                _isRewardSelectionPending = false;
                CompleteSettlementPhase();
                return;
            }

            StartCoroutine(PlayRewardCardIntoHandAndComplete(e));
        }

        private IEnumerator PlayRewardCardIntoHandAndComplete(GameEvents.CardRewardSelected selection)
        {
            var addedCard = Deck.DeckManager.Instance.AddCardToHand(selection.ChosenCard);
            if (UIManager.Instance != null && UIManager.Instance.handPanel != null && addedCard != null)
            {
                Vector3? sourcePosition = selection.HasSourceWorldPosition ? selection.SourceWorldPosition : null;
                yield return UIManager.Instance.handPanel.PlayIncomingCard(addedCard, UIHandIncomingAnimationKind.RewardPick, sourcePosition);
            }

            if (RewardPickOutroSeconds > 0f)
            {
                yield return new WaitForSeconds(RewardPickOutroSeconds);
            }

            _isRewardSelectionPending = false;
            CompleteSettlementPhase();
        }


        private void CompleteSettlementPhase()
        {
            if (_settlementTurnEndedPublished || _isRewardSelectionPending)
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

        /// <summary>
        /// 展示三选一奖励�?
        ///
        /// 流程�?
        /// 1. �?rewardLibrary 获取奖励池卡�?
        /// 2. 根据 boosted 参数选择卡池�?
        ///    - boosted=true：使用稀有度 >= Rare 的卡牌（高稀有度池）
        ///    - boosted=false：使用完整奖励库（全稀有度�?
        ///    - 如果 boosted 卡池为空，降级到完整�?
        /// 3. 从卡池中随机选择 3 张不重复的卡�?
        ///    - BuildUniqueRewardOptions 确保选出的卡片互不相�?
        /// 4. 发布 CardRewardOffered 事件，UI 监听并展示三个选项
        /// 5. 设置 _isRewardSelectionPending = true，等待玩家选择
        /// 6. 玩家选择后，UI 发布 CardRewardSelected 事件，由 OnCardRewardSelected 处理
        ///
        /// 错误处理�?
        /// - rewardLibrary 为空：记录警告，返回（无奖励�?
        /// - 奖励库卡牌不�?3 张：按实际数量展示（如只�?1 张则展示 1 个选项�?
        /// - UIManager 不存在：抛异常（必需�?
        ///
        /// 关键细节�?
        /// - boosted 状态由 ExecuteSettlePhase 根据贷款周期缓存
        /// - CardRewardSelected 事件会设�?_isRewardSelectionPending = false，完成循�?
        /// - 如果 AwardOneCardFromThreeOptions 没有设置 _isRewardSelectionPending�?
        ///   �?TryStartRewardOrComplete 会直接进�?CompleteSettlementPhase
        /// </summary>
        private void AwardOneCardFromThreeOptions(bool boosted)
        {
            EnsureEventSubscriptions();
            var rewardLibrary = GameManager.Instance != null && GameManager.Instance.gameConfig != null
                ? GameManager.Instance.gameConfig.rewardLibrary
                : null;

            if (rewardLibrary == null)
            {
                Debug.LogWarning("[TurnManager] No reward library configured.");
                return;
            }

            var allCards = rewardLibrary.entries;
            List<CardData> source;
            if (boosted)
            {
                source = allCards.ConvertAll(e => e.card).FindAll(c => c.rarity >= CardRarity.Rare);
                if (source.Count == 0)
                {
                    Debug.LogWarning("[TurnManager] Boosted reward requested but reward library has no Rare+ cards. Falling back to full reward library.");
                    source = allCards.ConvertAll(e => e.card);
                }
            }
            else
            {
                source = allCards.ConvertAll(e => e.card);
            }

            if (source.Count == 0)
            {
                Debug.LogWarning("[TurnManager] No reward cards available.");
                return;
            }

            if (UIManager.Instance == null)
            {
                throw new InvalidOperationException("[TurnManager] Reward selection requires UIManager in scene.");
            }

            var options = BuildUniqueCardOptions(source, 3);
            if (options.Length == 0)
            {
                Debug.LogWarning("[TurnManager] No unique reward cards available.");
                return;
            }

            if (options.Length < 3)
            {
                Debug.LogWarning($"[TurnManager] Reward library only has {options.Length} unique reward card(s) available. Offering {options.Length} unique option(s).");
            }

            // 设置等待状态，�?UI 发布 CardRewardSelected 事件后恢�?
            _isRewardSelectionPending = true;
            EventBus.Publish(new GameEvents.CardRewardOffered
            {
                Options = options,
                Boosted = boosted
            });
        }

        private void EnsureEventSubscriptions()
        {
            if (_eventsSubscribed)
            {
                return;
            }

            EventBus.Subscribe<GameEvents.SettlementPlaybackCompleted>(OnSettlementPlaybackCompleted);
            EventBus.Subscribe<GameEvents.CardRewardSelected>(OnCardRewardSelected);
            _eventsSubscribed = true;
        }

        private static CardData[] BuildUniqueCardOptions(List<CardData> source, int desiredCount)
        {
            var remaining = source != null
                ? source.Where(card => card != null).ToList()
                : new List<CardData>();
            var options = new List<CardData>(Mathf.Max(0, desiredCount));

            while (remaining.Count > 0 && options.Count < desiredCount)
            {
                var chosen = remaining[UnityEngine.Random.Range(0, remaining.Count)];
                options.Add(chosen);
                remaining.RemoveAll(card => IsSameRewardCard(card, chosen));
            }

            return options.ToArray();
        }

        private static bool IsSameRewardCard(CardData left, CardData right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return ReferenceEquals(left, right) || left.cardId == right.cardId;
        }
    }
}

