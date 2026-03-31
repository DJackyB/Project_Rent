using System.Collections.Generic;
using UnityEngine;

namespace Martian.RandomEvent
{
    [CreateAssetMenu(fileName = "NewRandomEventLibrary", menuName = "Martian/Random Event Library")]
    public class RandomEventLibrary : ScriptableObject
    {
        [Tooltip("Stable identifier used for runtime lookup.")]
        public string libraryId;

        [Tooltip("Optional display name for inspector and logs.")]
        public string displayName;

        [Tooltip("Event entries. Repeated entries count as extra weight.")]
        public List<RandomEventData> events = new();

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}
