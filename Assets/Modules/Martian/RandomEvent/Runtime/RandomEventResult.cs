namespace Martian.RandomEvent
{
    /// <summary>
    /// Immutable result delivered to callers after the player selects an option.
    /// </summary>
    public readonly struct RandomEventResult
    {
        public readonly RandomEventData EventData;
        public readonly string EventInstanceId;
        public readonly string SelectedOptionId;
        public readonly int SelectedOptionIndex;

        public RandomEventResult(RandomEventData eventData, string eventInstanceId,
            string selectedOptionId, int selectedOptionIndex)
        {
            EventData = eventData;
            EventInstanceId = eventInstanceId;
            SelectedOptionId = selectedOptionId;
            SelectedOptionIndex = selectedOptionIndex;
        }
    }
}
