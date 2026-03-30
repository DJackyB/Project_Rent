using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// Card library registry used by startup validation and effect-time library lookup.
    /// </summary>
    public static class CardLibraryDatabase
    {
        private static readonly Dictionary<string, CardLibrary> _libraries = new();
        private static bool _isLoaded;

        public static bool IsLoaded => _isLoaded;

        public static void LoadAll(string resourcePath = "CardLibraries")
        {
            _libraries.Clear();

            var libraries = Resources.LoadAll<CardLibrary>(resourcePath);
            foreach (var library in libraries)
            {
                RegisterInternal(library, "LoadAll");
            }

            _isLoaded = true;
            Debug.Log($"[CardLibraryDatabase] Loaded {_libraries.Count} libraries.");
        }

        public static void Register(CardLibrary library)
        {
            RegisterInternal(library, "Register");
            _isLoaded = true;
        }

        public static CardLibrary GetById(string libraryId)
        {
            EnsureLoaded();

            if (!_libraries.TryGetValue(libraryId, out var library))
            {
                throw new InvalidOperationException($"[CardLibraryDatabase] Library '{libraryId}' not found.");
            }

            return library;
        }

        public static bool TryGetById(string libraryId, out CardLibrary library)
        {
            EnsureLoaded();
            return _libraries.TryGetValue(libraryId, out library);
        }

        public static IReadOnlyDictionary<string, CardLibrary> GetAll()
        {
            EnsureLoaded();
            return _libraries;
        }

        public static void Clear()
        {
            _libraries.Clear();
            _isLoaded = false;
        }

        public static void ValidateLibrary(CardLibrary library, string sourceLabel)
        {
            if (library == null)
            {
                throw new InvalidOperationException($"[CardLibraryDatabase] {sourceLabel}: library is null.");
            }

            if (string.IsNullOrWhiteSpace(library.libraryId))
            {
                throw new InvalidOperationException($"[CardLibraryDatabase] {sourceLabel}: libraryId is empty.");
            }

            if (library.cards == null)
            {
                throw new InvalidOperationException($"[CardLibraryDatabase] {sourceLabel}: cards list is null.");
            }

            for (int i = 0; i < library.cards.Count; i++)
            {
                if (library.cards[i] == null)
                {
                    throw new InvalidOperationException(
                        $"[CardLibraryDatabase] {sourceLabel}: contains a null card entry at index {i}.");
                }
            }
        }

        private static void RegisterInternal(CardLibrary library, string sourceLabel)
        {
            ValidateLibrary(library, sourceLabel);

            if (_libraries.TryGetValue(library.libraryId, out var existing) && existing != library)
            {
                throw new InvalidOperationException(
                    $"[CardLibraryDatabase] Duplicate libraryId detected: {library.libraryId}. Existing={existing.name}, Incoming={library.name}");
            }

            _libraries[library.libraryId] = library;
        }

        private static void EnsureLoaded()
        {
            if (!_isLoaded)
            {
                throw new InvalidOperationException("[CardLibraryDatabase] Accessed before LoadAll().");
            }
        }
    }
}
