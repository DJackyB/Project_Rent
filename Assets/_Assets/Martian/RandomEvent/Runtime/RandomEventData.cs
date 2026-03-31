using System;
using System.Collections.Generic;
using UnityEngine;

namespace Martian.RandomEvent
{
    [CreateAssetMenu(fileName = "NewRandomEvent", menuName = "Martian/Random Event Data")]
    public class RandomEventData : ScriptableObject
    {
        [Tooltip("Unique event identifier.")]
        public string eventId;

        [Tooltip("Optional tags for business-side filtering. The module does not interpret them.")]
        public string[] tags = Array.Empty<string>();

        [Tooltip("Event title. Supports {0} placeholders filled at trigger time.")]
        public string titleKey;

        [Tooltip("Event description. Supports {0} {1} placeholders filled at trigger time.")]
        [TextArea(3, 6)]
        public string descriptionKey;

        [Tooltip("Event artwork / icon.")]
        public Sprite artwork;

        [Tooltip("Player-selectable options.")]
        public List<RandomEventOption> options = new();
    }

    [Serializable]
    public class RandomEventOption
    {
        [Tooltip("Unique option identifier within this event.")]
        public string optionId;

        [Tooltip("Option button text. Supports {0} placeholders.")]
        public string displayTextKey;

        [Tooltip("Optional tooltip explaining consequences.")]
        public string tooltipKey;
    }
}
