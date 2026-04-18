using BaoZuPo.Card;
using BaoZuPo.Core;
using Martian.EventBus;
using Martian.RandomEvent;
using UnityEngine;

namespace BaoZuPo.GameFlow
{
    /// <summary>
    /// 随机事件选项效果执行桥接器。
    /// 订阅 RandomEventOptionSelected，执行选项的 effect 字符串，并处理事件链。
    /// 挂在场景 Managers 节点上。
    /// </summary>
    public class RandomEventEffectBridge : MonoBehaviour
    {
        private RandomEventData _currentEventData;

        private void OnEnable()
        {
            EventBus.Subscribe<RandomEventTriggered>(OnEventTriggered);
            EventBus.Subscribe<RandomEventOptionSelected>(OnOptionSelected);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RandomEventTriggered>(OnEventTriggered);
            EventBus.Unsubscribe<RandomEventOptionSelected>(OnOptionSelected);
        }

        private void OnEventTriggered(RandomEventTriggered msg)
        {
            _currentEventData = msg.DisplayData.SourceData;
        }

        private void OnOptionSelected(RandomEventOptionSelected msg)
        {
            if (_currentEventData == null)
            {
                Debug.LogWarning("[RandomEventEffectBridge] OnOptionSelected fired but no current event cached.");
                return;
            }

            var option = _currentEventData.options.Find(o => o.optionId == msg.SelectedOptionId);
            if (option == null)
            {
                Debug.LogWarning($"[RandomEventEffectBridge] Option '{msg.SelectedOptionId}' not found in event '{_currentEventData.eventId}'.");
                _currentEventData = null;
                return;
            }

            var context = GameManager.Instance?.GameContext;

            if (!string.IsNullOrWhiteSpace(option.effect))
            {
                if (context == null)
                    Debug.LogWarning("[RandomEventEffectBridge] GameContext is null, skipping effect execution.");
                else
                    CardEffectFactory.Execute(option.effect, context);
            }

            var nextId = option.nextEventId;
            _currentEventData = null;

            if (!string.IsNullOrWhiteSpace(nextId))
                RandomEventManager.Instance?.TriggerEvent(nextId);
        }
    }
}
