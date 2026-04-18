using System;

namespace Martian.RandomEvent
{
    /// <summary>
    /// Encapsulates all parameters for triggering a random event.
    /// Avoids method-overload explosion on RandomEventManager.
    /// </summary>
    public class RandomEventRequest
    {
        /// <summary>The event definition to display. Required.</summary>
        public RandomEventData Data { get; set; }

        /// <summary>
        /// Optional format arguments applied to titleKey, descriptionKey,
        /// and option displayTextKey / tooltipKey via string.Format.
        /// </summary>
        public object[] DescriptionArgs { get; set; }

        /// <summary>
        /// Optional filter evaluated per option. Return true = available, false = greyed out.
        /// When null, all options are available.
        /// </summary>
        public Func<RandomEventOption, bool> OptionFilter { get; set; }

        /// <summary>
        /// Optional callback invoked when the player selects an option.
        /// The EventBus message is always published regardless of this callback.
        /// </summary>
        public Action<RandomEventResult> OnComplete { get; set; }
    }
}
