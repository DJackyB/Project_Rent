using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaoZuPo.Card
{
    /// <summary>
    /// 卡牌库数据库。
    ///
    /// 从 Resources/CardLibraries/ 目录加载所有 CardLibrary 资源，并提供库 ID 索引访问。
    /// 用于效果字符串（如 DrawCard;2;EventPool）在执行时快速查询指定的卡池。
    ///
    /// 特点：
    /// - 按库 ID（string）索引存储和查询
    /// - 启动时验证所有库的完整性（检测 null、空 ID、null 条目）
    /// - 检测并报错重复库 ID，防止配置冲突
    /// - 配置错误直接抛异常，不做静默 fallback
    /// </summary>
    public static class CardLibraryDatabase
    {
        /// <summary>库 ID 到 CardLibrary 的映射表。</summary>
        private static readonly Dictionary<string, CardLibrary> _libraries = new();

        /// <summary>是否已加载过数据。</summary>
        private static bool _isLoaded;

        /// <summary>是否已初始化（LoadAll 或 Register 被调用过）。</summary>
        public static bool IsLoaded => _isLoaded;

        /// <summary>
        /// 从 Resources 目录加载所有卡牌库。
        ///
        /// 应在游戏启动早期调用一次。加载时会验证每个库的完整性。
        /// 加载失败时会直接抛异常。
        /// </summary>
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

        /// <summary>
        /// 手动注册一个卡牌库（用于运行时或测试）。
        /// </summary>
        public static void Register(CardLibrary library)
        {
            RegisterInternal(library, "Register");
            _isLoaded = true;
        }

        /// <summary>
        /// 按库 ID 查询库。
        ///
        /// 异常：
        /// - 未调用 LoadAll()：InvalidOperationException
        /// - 库 ID 不存在：InvalidOperationException
        /// </summary>
        public static CardLibrary GetById(string libraryId)
        {
            EnsureLoaded();

            if (!_libraries.TryGetValue(libraryId, out var library))
            {
                throw new InvalidOperationException($"[CardLibraryDatabase] Library '{libraryId}' not found.");
            }

            return library;
        }

        /// <summary>
        /// 尝试按库 ID 查询库。
        /// </summary>
        public static bool TryGetById(string libraryId, out CardLibrary library)
        {
            EnsureLoaded();
            return _libraries.TryGetValue(libraryId, out library);
        }

        /// <summary>
        /// 返回所有已加载的库（只读）。
        /// </summary>
        public static IReadOnlyDictionary<string, CardLibrary> GetAll()
        {
            EnsureLoaded();
            return _libraries;
        }

        /// <summary>清空所有数据，重置加载状态。通常用于测试清理。</summary>
        public static void Clear()
        {
            _libraries.Clear();
            _isLoaded = false;
        }

        /// <summary>
        /// 验证库的完整性。检查 null、空 ID、null 条目等。
        ///
        /// 异常：
        /// - 库为 null：InvalidOperationException
        /// - 库 ID 为空：InvalidOperationException
        /// - entries 列表为 null：InvalidOperationException
        /// - 某条目或其 card 为 null：InvalidOperationException（包含索引）
        /// </summary>
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

            if (library.entries == null)
            {
                throw new InvalidOperationException($"[CardLibraryDatabase] {sourceLabel}: entries list is null in library '{library.libraryId}'.");
            }

            for (int i = 0; i < library.entries.Count; i++)
            {
                var entry = library.entries[i];
                if (entry == null)
                {
                    throw new InvalidOperationException(
                        $"[CardLibraryDatabase] {sourceLabel}: library '{library.libraryId}' has a null entry at index {i}.");
                }

                if (entry.card == null)
                {
                    throw new InvalidOperationException(
                        $"[CardLibraryDatabase] {sourceLabel}: library '{library.libraryId}' entry at index {i} has a null card.");
                }

                if (entry.quantity <= 0)
                {
                    throw new InvalidOperationException(
                        $"[CardLibraryDatabase] {sourceLabel}: library '{library.libraryId}' entry at index {i} has non-positive quantity {entry.quantity}.");
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
