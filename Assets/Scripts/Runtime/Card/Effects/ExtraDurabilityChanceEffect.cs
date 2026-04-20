using UnityEngine;

namespace BaoZuPo.Card.Effects
{
    /// <summary>
    /// 在结算阶段，以指定概率额外损耗一点耐久，若降至 0 则标记销毁。
    /// 格式：ExtraDurabilityChance;0.3
    /// </summary>
    public class ExtraDurabilityChanceEffect : ICardEffect
    {
        private readonly float _probability;

        public ExtraDurabilityChanceEffect(float probability)
        {
            _probability = probability;
        }

        public void Execute(CardInstance source, GameContext context)
        {
            if (source == null || source.IsDestroyed || source.Data == null || source.Data.durability <= 0)
            {
                return;
            }

            if (Random.value >= _probability)
            {
                return;
            }

            source.CurrentDurability--;
            Debug.Log($"[Effect] {source.Data.cardName}: ExtraDurabilityChance triggered, durability now {source.CurrentDurability}");

            if (source.CurrentDurability <= 0)
            {
                source.MarkDestroyed();
                Debug.Log($"[Effect] {source.Data.cardName}: Destroyed by extra durability loss.");
            }
        }
    }
}
