using System.Collections.ObjectModel;

namespace Martian.RandomEvent
{
    // ── Published by RandomEventManager when an event is ready for display ───

    /// <summary>
    /// EventBus message: a random event is ready for UI display.
    /// UI subscribes to this, shows the popup, and publishes
    /// <see cref="RandomEventOptionSelected"/> when the player picks an option.
    /// </summary>
    public sealed class RandomEventTriggered
    {
        public string EventInstanceId { get; }
        public RandomEventDisplayData DisplayData { get; }

        public RandomEventTriggered(string eventInstanceId, RandomEventDisplayData displayData)
        {
            EventInstanceId = eventInstanceId;
            DisplayData = displayData;
        }
    }

    // ── Display payload consumed by UI ───────────────────────────────────────

    /// <summary>
    /// Read-only data package that UI consumes directly.
    /// All text fields are already formatted (placeholders resolved).
    /// </summary>
    public readonly struct RandomEventDisplayData
    {
        /// <summary>Original SO reference (for artwork, tags, etc.).</summary>
        public readonly RandomEventData SourceData;
        public readonly string EventInstanceId;
        public readonly string FormattedTitle;
        public readonly string FormattedDescription;
        public readonly ReadOnlyCollection<RandomEventOptionDisplay> Options;

        public RandomEventDisplayData(
            RandomEventData sourceData,
            string eventInstanceId,
            string formattedTitle,
            string formattedDescription,
            ReadOnlyCollection<RandomEventOptionDisplay> options)
        {
            SourceData = sourceData;
            EventInstanceId = eventInstanceId;
            FormattedTitle = formattedTitle;
            FormattedDescription = formattedDescription;
            Options = options;
        }
    }

    /// <summary>
    /// Per-option display state: formatted text + availability from OptionFilter.
    /// </summary>
    public readonly struct RandomEventOptionDisplay
    {
        public readonly string OptionId;
        public readonly string FormattedDisplayText;
        public readonly string FormattedTooltip;
        /// <summary>True if the option passed OptionFilter (or no filter was set).</summary>
        public readonly bool IsAvailable;

        public RandomEventOptionDisplay(
            string optionId,
            string formattedDisplayText,
            string formattedTooltip,
            bool isAvailable)
        {
            OptionId = optionId;
            FormattedDisplayText = formattedDisplayText;
            FormattedTooltip = formattedTooltip;
            IsAvailable = isAvailable;
        }
    }

    // ── Published by UI when the player selects an option ────────────────────

    /// <summary>
    /// EventBus message: the player selected an option in the event popup.
    /// Published by the UI panel; consumed by RandomEventManager (and optionally by business code).
    /// </summary>
    public struct RandomEventOptionSelected
    {
        public string EventInstanceId;
        public string SelectedOptionId;
        public int SelectedOptionIndex;
    }
}
