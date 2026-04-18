using Martian.RandomEvent;
using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 效果格式：TriggerRandomEvent;libraryId
    /// 示例：TriggerRandomEvent;lib_general
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
            var manager = RandomEventManager.Instance;
            if (manager == null)
            {
                Debug.LogError("[TriggerRandomEventEffect] RandomEventManager not found in scene.");
                return;
            }

            manager.TriggerRandomFromLibrary(_libraryId);
        }
    }
}
