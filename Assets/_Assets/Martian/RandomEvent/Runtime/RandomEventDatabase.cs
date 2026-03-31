using System;
using System.Collections.Generic;
using UnityEngine;

namespace Martian.RandomEvent
{
    /// <summary>
    /// Static registry for RandomEventData and RandomEventLibrary assets.
    /// Must be explicitly initialised via LoadAll() before use.
    /// </summary>
    public static class RandomEventDatabase
    {
        private static readonly Dictionary<string, RandomEventData> _events = new();
        private static readonly Dictionary<string, RandomEventLibrary> _libraries = new();
        private static bool _isLoaded;

        public static bool IsLoaded => _isLoaded;

        // ── Loading ──────────────────────────────────────────────

        public static void LoadAll(
            string eventResourcePath = "RandomEvents",
            string libraryResourcePath = "RandomEventLibraries")
        {
            _events.Clear();
            _libraries.Clear();

            var allEvents = Resources.LoadAll<RandomEventData>(eventResourcePath);
            foreach (var evt in allEvents)
            {
                RegisterEvent(evt, "LoadAll");
            }

            var allLibraries = Resources.LoadAll<RandomEventLibrary>(libraryResourcePath);
            foreach (var lib in allLibraries)
            {
                RegisterLibrary(lib, "LoadAll");
            }

            _isLoaded = true;
            Debug.Log($"[RandomEventDatabase] Loaded {_events.Count} events, {_libraries.Count} libraries.");
        }

        // ── Event Lookup ─────────────────────────────────────────

        public static RandomEventData GetEventById(string eventId)
        {
            EnsureLoaded();

            if (_events.TryGetValue(eventId, out var data))
                return data;

            Debug.LogWarning($"[RandomEventDatabase] Event '{eventId}' not found.");
            return null;
        }

        public static bool TryGetEventById(string eventId, out RandomEventData data)
        {
            EnsureLoaded();
            return _events.TryGetValue(eventId, out data);
        }

        public static IReadOnlyDictionary<string, RandomEventData> GetAllEvents()
        {
            EnsureLoaded();
            return _events;
        }

        // ── Library Lookup ───────────────────────────────────────

        public static RandomEventLibrary GetLibraryById(string libraryId)
        {
            EnsureLoaded();

            if (_libraries.TryGetValue(libraryId, out var lib))
                return lib;

            Debug.LogWarning($"[RandomEventDatabase] Library '{libraryId}' not found.");
            return null;
        }

        public static bool TryGetLibraryById(string libraryId, out RandomEventLibrary library)
        {
            EnsureLoaded();
            return _libraries.TryGetValue(libraryId, out library);
        }

        public static IReadOnlyDictionary<string, RandomEventLibrary> GetAllLibraries()
        {
            EnsureLoaded();
            return _libraries;
        }

        // ── Manual Registration ──────────────────────────────────

        public static void RegisterEvent(RandomEventData data, string sourceLabel = "Register")
        {
            ValidateEvent(data, sourceLabel);

            if (_events.TryGetValue(data.eventId, out var existing) && existing != data)
            {
                throw new InvalidOperationException(
                    $"[RandomEventDatabase] {sourceLabel}: Duplicate eventId '{data.eventId}'. " +
                    $"Existing={existing.name}, Incoming={data.name}");
            }

            _events[data.eventId] = data;
        }

        public static void RegisterLibrary(RandomEventLibrary library, string sourceLabel = "Register")
        {
            ValidateLibrary(library, sourceLabel);

            if (_libraries.TryGetValue(library.libraryId, out var existing) && existing != library)
            {
                throw new InvalidOperationException(
                    $"[RandomEventDatabase] {sourceLabel}: Duplicate libraryId '{library.libraryId}'. " +
                    $"Existing={existing.name}, Incoming={library.name}");
            }

            _libraries[library.libraryId] = library;
        }

        // ── Validation ───────────────────────────────────────────

        public static void ValidateEvent(RandomEventData data, string sourceLabel)
        {
            if (data == null)
                throw new InvalidOperationException(
                    $"[RandomEventDatabase] {sourceLabel}: event is null.");

            if (string.IsNullOrWhiteSpace(data.eventId))
                throw new InvalidOperationException(
                    $"[RandomEventDatabase] {sourceLabel}: eventId is empty on asset '{data.name}'.");

            if (data.options == null || data.options.Count == 0)
                throw new InvalidOperationException(
                    $"[RandomEventDatabase] {sourceLabel}: event '{data.eventId}' has no options.");
        }

        public static void ValidateLibrary(RandomEventLibrary library, string sourceLabel)
        {
            if (library == null)
                throw new InvalidOperationException(
                    $"[RandomEventDatabase] {sourceLabel}: library is null.");

            if (string.IsNullOrWhiteSpace(library.libraryId))
                throw new InvalidOperationException(
                    $"[RandomEventDatabase] {sourceLabel}: libraryId is empty on asset '{library.name}'.");

            if (library.events == null)
                throw new InvalidOperationException(
                    $"[RandomEventDatabase] {sourceLabel}: events list is null on library '{library.libraryId}'.");

            for (int i = 0; i < library.events.Count; i++)
            {
                if (library.events[i] == null)
                    throw new InvalidOperationException(
                        $"[RandomEventDatabase] {sourceLabel}: library '{library.libraryId}' has null entry at index {i}.");
            }
        }

        // ── Cleanup ──────────────────────────────────────────────

        public static void Clear()
        {
            _events.Clear();
            _libraries.Clear();
            _isLoaded = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnDomainReload()
        {
            Clear();
        }

        // ── Internal ─────────────────────────────────────────────

        private static void EnsureLoaded()
        {
            if (!_isLoaded)
                throw new InvalidOperationException(
                    "[RandomEventDatabase] Accessed before LoadAll(). " +
                    "Call RandomEventDatabase.LoadAll() during project initialisation.");
        }
    }
}
