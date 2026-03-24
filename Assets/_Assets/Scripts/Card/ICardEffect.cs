namespace BaoZuPo.Card
{
    /// <summary>
    /// 效果执行时的附加上下文。
    /// 一次出牌或一次效果链共用同一个上下文对象。
    /// </summary>
    public class EffectExecutionContext
    {
        /// <summary>当前选择的房间目标，可为空</summary>
        public Board.RoomSlot SelectedRoom { get; set; }
    }

    /// <summary>
    /// 卡牌效果接口。
    /// 所有卡牌效果（如 AddMoney、ReduceMoney）都需要实现此接口。
    /// </summary>
    public interface ICardEffect
    {
        /// <summary>
        /// 执行效果。
        /// </summary>
        /// <param name="source">触发该效果的卡牌实例</param>
        /// <param name="context">游戏上下文，提供对各系统的访问能力</param>
        void Execute(CardInstance source, GameContext context);
    }

    /// <summary>
    /// 游戏上下文，供效果执行时使用。
    /// 用来避免效果直接依赖各类 Manager 单例，便于测试和扩展。
    /// </summary>
    public class GameContext
    {
        public Economy.MoneyManager MoneyManager { get; set; }
        public Board.BoardManager BoardManager { get; set; }
        public EffectExecutionContext EffectContext { get; set; } = new();

        // 后续如有需要，可以继续扩展更多系统引用
    }
}
