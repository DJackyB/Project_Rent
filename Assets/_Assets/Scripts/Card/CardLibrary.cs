using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Card
{
    [CreateAssetMenu(fileName = "NewCardLibrary", menuName = "BaoZuPo/Card Library")]
    public class CardLibrary : ScriptableObject
    {
        [Tooltip("Stable id used by effect strings such as DrawCard;2;EventPool.")]
        public string libraryId;

        [Tooltip("Optional display name used in inspector and logs.")]
        public string displayName;

        [Tooltip("Explicit card list. Repeated entries count as extra copies/weight.")]
        public List<CardData> cards = new();

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}
