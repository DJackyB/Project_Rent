using BaoZuPo.Card;
using UnityEngine;

namespace BaoZuPo.GameFlow
{
    public readonly struct RewardChoiceResult
    {
        public static RewardChoiceResult Skipped => default;

        public readonly CardData ChosenCard;
        public readonly bool HasSourceWorldPosition;
        public readonly Vector3 SourceWorldPosition;

        public RewardChoiceResult(CardData chosenCard, bool hasSourceWorldPosition, Vector3 sourceWorldPosition)
        {
            ChosenCard = chosenCard;
            HasSourceWorldPosition = hasSourceWorldPosition;
            SourceWorldPosition = sourceWorldPosition;
        }
    }
}
