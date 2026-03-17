namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌效果接口
    /// 所有卡牌效果（如 AddMoney、ReduceMoney）都实现此接口。
    /// </summary>
    public interface ICardEffect
    {
        /// <summary>
        /// 执行效果
        /// </summary>
        /// <param name="source">触发此效果的卡牌实例</param>
        /// <param name="context">游戏上下文，提供对各系统的访问</param>
        void Execute(CardInstance source, GameContext context);
    }

    /// <summary>
    /// 游戏上下文，传递给效果执行时使用。
    /// 避免效果直接引用各种 Manager 单例，方便测试和扩展。
    /// </summary>
    public class GameContext
    {
        public Economy.MoneyManager MoneyManager { get; set; }
        public Board.BoardManager BoardManager { get; set; }

        // 后续可扩展更多系统引用
    }
}
