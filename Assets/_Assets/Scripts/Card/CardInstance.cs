namespace BaoZuPo.Card
{
    /// <summary>
    /// 运行时卡牌实例。
    ///
    /// 由 CardData（静态配置）实例化而来，代表游戏中实际存在的一张卡牌。
    /// 包含动态状态（当前耐久度、等待回合数、所属房间、摧毁标志）和已解析的效果对象。
    ///
    /// 生命周期：
    /// 1. 构造时：从 CardData 读取效果字符串，通过 CardEffectFactory 创建 ICardEffect 对象
    /// 2. 游戏中：追踪当前状态（耐久度减少、等待倒计时）
    /// 3. 摧毁时：调用 MarkDestroyed() 标记，触发 DestroyEffect
    /// </summary>
    public class CardInstance
    {
        /// <summary>该实例对应的静态卡牌数据。</summary>
        public CardData Data { get; private set; }

        /// <summary>当前耐久度。在结算阶段消耗。</summary>
        public int CurrentDurability { get; set; }

        /// <summary>当前等待回合数。每回合结束时 -1。</summary>
        public int CurrentWait { get; set; }

        /// <summary>该卡牌当前所在的房间槽位（如果有的话），否则为 null。</summary>
        public Board.RoomSlot PlacedRoom { get; set; }

        /// <summary>是否已被标记为摧毁。</summary>
        public bool IsDestroyed { get; private set; }

        /// <summary>已解析的准备阶段效果。</summary>
        public ICardEffect PreEffect { get; private set; }

        /// <summary>已解析的即时效果。</summary>
        public ICardEffect InstantEffect { get; private set; }

        /// <summary>已解析的结算效果。</summary>
        public ICardEffect SettleEffect { get; private set; }

        /// <summary>已解析的摧毁效果。</summary>
        public ICardEffect DestroyEffect { get; private set; }

        /// <summary>
        /// 从 CardData 创建运行时实例。
        ///
        /// 初始化当前状态（耐久度、等待回合数、未摧毁），
        /// 并将 CardData 中的效果字符串通过 CardEffectFactory 解析为 ICardEffect 对象。
        /// </summary>
        public CardInstance(CardData data)
        {
            Data = data;
            CurrentDurability = data.durability;
            CurrentWait = data.waitTurns;
            IsDestroyed = false;

            PreEffect = CardEffectFactory.Create(data.preEffect);
            InstantEffect = CardEffectFactory.Create(data.instantEffect);
            SettleEffect = CardEffectFactory.Create(data.settleEffect);
            DestroyEffect = CardEffectFactory.Create(data.destroyEffect);
        }

        /// <summary>将卡牌标记为已摧毁。</summary>
        public void MarkDestroyed()
        {
            IsDestroyed = true;
        }

        /// <summary>返回人类可读的卡牌信息，包含名称、ID、当前耐久度、等待回合数。</summary>
        public override string ToString()
        {
            return $"[{Data.cardName}](ID:{Data.cardId}, Durability:{CurrentDurability}, Wait:{CurrentWait})";
        }
    }
}
