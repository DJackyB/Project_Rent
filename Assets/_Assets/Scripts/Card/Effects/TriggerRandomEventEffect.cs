using Martian.RandomEvent;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 触发随机事件效果。从指定的 RandomEventLibrary 中加权随机抽取一个事件并触发。
    ///
    /// 效果格式：TriggerRandomEvent;libraryId
    /// 示例：TriggerRandomEvent;act1_events
    ///
    /// 注意：事件触发后效果的执行（金钱/状态变化等）由业务层通过 OnComplete 回调实现，
    /// 本效果仅负责触发弹出事件选择。UI 面板需订阅 RandomEventTriggered 消息。
    /// </summary>
    public class TriggerRandomEventEffect : ICardEffect
    {
        private readonly string _libraryId;

        public TriggerRandomEventEffect(string libraryId)
        {
            _libraryId = libraryId;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            RandomEventManager.Instance?.TriggerRandomFromLibrary(_libraryId);
        }
    }
}
