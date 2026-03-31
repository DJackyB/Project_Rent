using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Martian.EventBus;
using UnityEngine;

namespace Martian.RandomEvent
{
    /// <summary>
    /// Core manager for the random event system.
    /// Handles event queuing, display-data preparation, and callback dispatch.
    /// Place this MonoBehaviour in the scene; access via RandomEventManager.Instance.
    /// </summary>
    public class RandomEventManager : MonoBehaviour
    {
        // ── Singleton (no base-class dependency) ─────────────────

        private static RandomEventManager _instance;

        public static RandomEventManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<RandomEventManager>();
                    if (_instance == null)
                        Debug.LogError(
                            "[RandomEventManager] No instance found in scene. " +
                            "Add a GameObject with RandomEventManager to your scene.");
                }

                return _instance;
            }
        }

        // ── Internal state ───────────────────────────────────────

        private readonly Queue<PendingEvent> _eventQueue = new();
        private PendingEvent _currentEvent;

        /// <summary>True if a popup is currently showing or events are queued.</summary>
        public bool IsEventActive => _currentEvent != null || _eventQueue.Count > 0;

        /// <summary>Number of events waiting (excluding the one currently displayed).</summary>
        public int QueuedCount => _eventQueue.Count;

        // ── Lifecycle ────────────────────────────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[RandomEventManager] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnEnable()
        {
            EventBus.EventBus.Subscribe<RandomEventOptionSelected>(OnOptionSelected);
        }

        private void OnDisable()
        {
            EventBus.EventBus.Unsubscribe<RandomEventOptionSelected>(OnOptionSelected);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        // ── Public API: TriggerEvent ─────────────────────────────

        /// <summary>Fire-and-forget by event ID.</summary>
        public void TriggerEvent(string eventId)
        {
            var data = RandomEventDatabase.GetEventById(eventId);
            if (data == null) return;
            TriggerEvent(new RandomEventRequest { Data = data });
        }

        /// <summary>Fire-and-forget by direct SO reference.</summary>
        public void TriggerEvent(RandomEventData data)
        {
            TriggerEvent(new RandomEventRequest { Data = data });
        }

        /// <summary>Full trigger with filter, args, and callback.</summary>
        public void TriggerEvent(RandomEventRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Data == null)
                throw new ArgumentNullException(nameof(request.Data),
                    "[RandomEventManager] RandomEventRequest.Data is null.");

            var pending = new PendingEvent
            {
                Data = request.Data,
                InstanceId = Guid.NewGuid().ToString(),
                DescriptionArgs = request.DescriptionArgs,
                OptionFilter = request.OptionFilter,
                OnComplete = request.OnComplete
            };

            _eventQueue.Enqueue(pending);

            if (_currentEvent == null)
                ShowNextEvent();
        }

        // ── Public API: TriggerRandomFromLibrary ─────────────────

        /// <summary>Pick a weighted-random event from a library and trigger it.</summary>
        public void TriggerRandomFromLibrary(string libraryId,
            Action<RandomEventResult> onComplete = null)
        {
            var library = RandomEventDatabase.GetLibraryById(libraryId);
            if (library == null) return;

            if (library.events == null || library.events.Count == 0)
            {
                Debug.LogWarning(
                    $"[RandomEventManager] Library '{libraryId}' has no events.");
                return;
            }

            // 重复条目 = 权重，直接随机索引
            var picked = library.events[UnityEngine.Random.Range(0, library.events.Count)];

            TriggerEvent(new RandomEventRequest
            {
                Data = picked,
                OnComplete = onComplete
            });
        }

        // ── Internal: queue progression ──────────────────────────

        private void ShowNextEvent()
        {
            if (_eventQueue.Count == 0)
            {
                _currentEvent = null;
                return;
            }

            _currentEvent = _eventQueue.Dequeue();

            var displayData = BuildDisplayData(_currentEvent);
            var message = new RandomEventTriggered(_currentEvent.InstanceId, displayData);

            EventBus.EventBus.Publish(message);
        }

        // ── Internal: build display payload ──────────────────────

        private RandomEventDisplayData BuildDisplayData(PendingEvent pending)
        {
            var args = pending.DescriptionArgs;
            var filter = pending.OptionFilter;
            var data = pending.Data;

            string formattedTitle = FormatSafe(data.titleKey, args);
            string formattedDescription = FormatSafe(data.descriptionKey, args);

            var optionDisplays = new List<RandomEventOptionDisplay>(data.options.Count);
            for (int i = 0; i < data.options.Count; i++)
            {
                var opt = data.options[i];
                bool isAvailable = filter == null || filter(opt);

                optionDisplays.Add(new RandomEventOptionDisplay(
                    opt.optionId,
                    FormatSafe(opt.displayTextKey, args),
                    FormatSafe(opt.tooltipKey, args),
                    isAvailable));
            }

            return new RandomEventDisplayData(
                data,
                pending.InstanceId,
                formattedTitle,
                formattedDescription,
                new ReadOnlyCollection<RandomEventOptionDisplay>(optionDisplays));
        }

        // ── Internal: option selected handler ────────────────────

        private void OnOptionSelected(RandomEventOptionSelected selected)
        {
            if (_currentEvent == null)
            {
                Debug.LogWarning(
                    "[RandomEventManager] Received RandomEventOptionSelected " +
                    "but no event is currently active. Ignoring.");
                return;
            }

            if (_currentEvent.InstanceId != selected.EventInstanceId)
            {
                Debug.LogWarning(
                    $"[RandomEventManager] InstanceId mismatch. " +
                    $"Expected={_currentEvent.InstanceId}, Got={selected.EventInstanceId}. Ignoring.");
                return;
            }

            var result = new RandomEventResult(
                _currentEvent.Data,
                _currentEvent.InstanceId,
                selected.SelectedOptionId,
                selected.SelectedOptionIndex);

            // 先调用回调，再推进队列（回调可能触发新事件入队）
            _currentEvent.OnComplete?.Invoke(result);
            _currentEvent = null;
            ShowNextEvent();
        }

        // ── Utility ──────────────────────────────────────────────

        /// <summary>
        /// string.Format with null/empty safety.
        /// Returns the original text if args is null/empty or format fails.
        /// </summary>
        private static string FormatSafe(string template, object[] args)
        {
            if (string.IsNullOrEmpty(template))
                return template ?? string.Empty;

            if (args == null || args.Length == 0)
                return template;

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                Debug.LogWarning(
                    $"[RandomEventManager] string.Format failed for template: \"{template}\"");
                return template;
            }
        }

        // ── Internal types ───────────────────────────────────────

        private class PendingEvent
        {
            public RandomEventData Data;
            public string InstanceId;
            public object[] DescriptionArgs;
            public Func<RandomEventOption, bool> OptionFilter;
            public Action<RandomEventResult> OnComplete;
        }
    }
}
