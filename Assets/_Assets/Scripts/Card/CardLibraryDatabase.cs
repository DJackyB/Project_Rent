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
    /// - 启动时验证所有库的完整性（检测 null、空 ID、null 卡牌）
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
        ///
        /// 参数：
        /// - resourcePath：资源路径（相对于 Assets/Resources/）。默认 "CardLibraries"
        ///
        /// 异常：
        /// - 库为 null
        /// - 库 ID 为空
        /// - 库卡牌列表为 null
        /// - 库中存在 null 卡牌
        /// - 重复库 ID
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
        ///
        /// 参数库会被验证。重复库 ID 会抛异常。
        ///
        /// 异常：同 ValidateLibrary
        /// </summary>
        public static void Register(CardLibrary library)
        {
            RegisterInternal(library, "Register");
            _isLoaded = true;
        }

        /// <summary>
        /// 按库 ID 查询库。
        ///
        /// 返回：CardLibrary 实例。
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
        ///
        /// 返回：true 如果找到，false 如果不存在。
        ///
        /// 异常：
        /// - 未调用 LoadAll()：InvalidOperationException
        /// </summary>
        public static bool TryGetById(string libraryId, out CardLibrary library)
        {
            EnsureLoaded();
            return _libraries.TryGetValue(libraryId, out library);
        }

        /// <summary>
        /// 返回所有已加载的库（只读）。
        ///
        /// 返回：ID 到 CardLibrary 的不可修改映射。
        ///
        /// 异常：
        /// - 未调用 LoadAll()：InvalidOperationException
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
        /// 验证库的完整性。检查 null、空 ID、null 卡牌等。
        ///
        /// 参数：
        /// - library：要验证的库
        /// - sourceLabel：错误日志中的源标签（如 "LoadAll" 或 "Register"）
        ///
        /// 异常：
        /// - 库为 null：InvalidOperationException
        /// - 库 ID 为空：InvalidOperationException
        /// - 卡牌列表为 null：InvalidOperationException
        /// - 某卡牌为 null：InvalidOperationException（包含索引）
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

        /// <summary>
        /// 内部方法，注册库并进行验证。
        ///
        /// 检测重复库 ID。
        /// </summary>
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

        /// <summary>内部方法，检查是否已初始化。未初始化时抛异常。</summary>
        private static void EnsureLoaded()
        {
            if (!_isLoaded)
            {
                throw new InvalidOperationException("[CardLibraryDatabase] Accessed before LoadAll().");
            }
        }
    }
}
