using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BaoZuPo.Card;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace BaoZuPo.Editor
{
    /// <summary>
    /// Keeps the Card string table in sync with CardData and provides a CSV workflow for translators.
    /// </summary>
    public static class CardLocalizationSyncUtility
    {
        private const string LocalizationAssetDir = "Assets/_Assets/Data/Localization";
        private const string CollectionName = "Card";
        private const string CsvPath = LocalizationAssetDir + "/Card.csv";
        private const string EnLocaleCode = "en";
        private const string ZhLocaleCode = "zh-Hans";

        [MenuItem("Tools/BaoZuPo/文本/同步卡牌双语表")]
        public static void SyncCardTablesMenu()
        {
            SyncCardTablesFromCardData();
        }

        [MenuItem("Tools/BaoZuPo/文本/导出卡牌翻译 CSV")]
        public static void ExportCardCsvMenu()
        {
            SyncCardTablesFromCardData();
            EditorUtility.RevealInFinder(CsvPath);
        }

        [MenuItem("Tools/BaoZuPo/文本/导入卡牌翻译 CSV")]
        public static void ImportCardCsvMenu()
        {
            ImportCardCsv();
        }

        public static void SyncCardTablesFromCardData(bool exportCsv = true, bool logSummary = true)
        {
            EnsureLocalizationDir();

            List<CardTextRow> rows = BuildRowsFromCardData();
            Locale enLocale = GetOrCreateLocale(EnLocaleCode);
            Locale zhLocale = GetOrCreateLocale(ZhLocaleCode);
            StringTableCollection collection = EnsureCollection(enLocale, zhLocale);
            StringTable enTable = collection.GetTable(enLocale.Identifier) as StringTable;
            StringTable zhTable = collection.GetTable(zhLocale.Identifier) as StringTable;

            if (enTable == null || zhTable == null)
            {
                throw new InvalidOperationException("[CardLocalizationSync] Failed to access Card string tables.");
            }

            HashSet<string> liveKeys = new HashSet<string>(rows.Select(row => row.Key), StringComparer.Ordinal);
            RemoveStaleEntries(collection, liveKeys);

            foreach (CardTextRow row in rows)
            {
                UpsertEntry(zhTable, row.Key, row.ZhHans);
                row.En = GetOrCreateEntry(enTable, row.Key).Value ?? string.Empty;
            }

            EditorUtility.SetDirty(enTable);
            EditorUtility.SetDirty(zhTable);
            if (collection.SharedData != null)
            {
                EditorUtility.SetDirty(collection.SharedData);
            }

            EditorUtility.SetDirty(collection);

            if (exportCsv)
            {
                WriteCsv(rows);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (logSummary)
            {
                Debug.Log($"[CardLocalizationSync] Synced {rows.Count} entries to Card tables and {(exportCsv ? "updated Card.csv." : "skipped CSV export.")}");
            }
        }

        public static void ImportCardCsv(bool logSummary = true)
        {
            EnsureLocalizationDir();
            SyncCardTablesFromCardData(exportCsv: false, logSummary: false);

            if (!File.Exists(CsvPath))
            {
                Debug.LogError($"[CardLocalizationSync] Card CSV not found at '{CsvPath}'. Export it first.");
                return;
            }

            Locale enLocale = GetOrCreateLocale(EnLocaleCode);
            Locale zhLocale = GetOrCreateLocale(ZhLocaleCode);
            StringTableCollection collection = EnsureCollection(enLocale, zhLocale);
            StringTable enTable = collection.GetTable(enLocale.Identifier) as StringTable;
            if (enTable == null)
            {
                throw new InvalidOperationException("[CardLocalizationSync] Failed to access the English Card table.");
            }

            List<CardTextRow> currentRows = BuildRowsFromCardData();
            HashSet<string> liveKeys = new HashSet<string>(currentRows.Select(row => row.Key), StringComparer.Ordinal);
            Dictionary<string, CardTextRow> importedRows = ReadCsv();

            int updatedCount = 0;
            int ignoredCount = 0;

            foreach (KeyValuePair<string, CardTextRow> pair in importedRows)
            {
                if (!liveKeys.Contains(pair.Key))
                {
                    ignoredCount++;
                    continue;
                }

                StringTableEntry entry = GetOrCreateEntry(enTable, pair.Key);
                string nextValue = pair.Value.En ?? string.Empty;
                if (string.Equals(entry.Value, nextValue, StringComparison.Ordinal))
                {
                    continue;
                }

                entry.Value = nextValue;
                updatedCount++;
            }

            EditorUtility.SetDirty(enTable);
            if (collection.SharedData != null)
            {
                EditorUtility.SetDirty(collection.SharedData);
            }

            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();

            WriteCsv(BuildRowsFromTables(enTable));
            AssetDatabase.Refresh();

            if (logSummary)
            {
                Debug.Log($"[CardLocalizationSync] Imported Card.csv. Updated {updatedCount} English entries, ignored {ignoredCount} stale row(s).");
            }
        }

        private static List<CardTextRow> BuildRowsFromCardData()
        {
            string[] guids = AssetDatabase.FindAssets("t:CardData");
            List<CardData> cards = new List<CardData>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null)
                {
                    cards.Add(card);
                }
            }

            cards.Sort((left, right) =>
            {
                int idCompare = left.cardId.CompareTo(right.cardId);
                if (idCompare != 0)
                {
                    return idCompare;
                }

                return string.CompareOrdinal(left.name, right.name);
            });

            HashSet<int> seenCardIds = new HashSet<int>();
            List<CardTextRow> rows = new List<CardTextRow>(cards.Count * 2);

            foreach (CardData card in cards)
            {
                if (!seenCardIds.Add(card.cardId))
                {
                    throw new InvalidOperationException($"[CardLocalizationSync] Duplicate cardId detected: {card.cardId}. Fix CardData assets before syncing localization.");
                }

                rows.Add(new CardTextRow(card.cardId, "Name", BuildKey(card.cardId, "Name"), card.cardName ?? string.Empty));
                rows.Add(new CardTextRow(card.cardId, "Description", BuildKey(card.cardId, "Description"), card.description ?? string.Empty));
            }

            return rows;
        }

        private static List<CardTextRow> BuildRowsFromTables(StringTable enTable)
        {
            List<CardTextRow> rows = BuildRowsFromCardData();
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].En = enTable != null ? enTable.GetEntry(rows[i].Key)?.Value ?? string.Empty : string.Empty;
            }

            return rows;
        }

        private static string BuildKey(int cardId, string field)
        {
            return $"Card.{cardId}.{field}";
        }

        private static Locale GetOrCreateLocale(string localeCode)
        {
            Locale locale = LocalizationEditorSettings.GetLocale(localeCode);
            if (locale != null)
            {
                return locale;
            }

            string localePath = $"{LocalizationAssetDir}/Locale-{localeCode}.asset";
            locale = AssetDatabase.LoadAssetAtPath<Locale>(localePath);
            if (locale == null)
            {
                locale = Locale.CreateLocale(new LocaleIdentifier(localeCode));
                locale.name = localeCode;
                AssetDatabase.CreateAsset(locale, localePath);
            }

            if (LocalizationEditorSettings.GetLocale(localeCode) == null)
            {
                LocalizationEditorSettings.AddLocale(locale);
            }

            EditorUtility.SetDirty(locale);
            return locale;
        }

        private static StringTableCollection EnsureCollection(params Locale[] locales)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(CollectionName);

            if (collection != null)
            {
                string collectionPath = AssetDatabase.GetAssetPath(collection);
                if (!string.IsNullOrEmpty(collectionPath) && !collectionPath.StartsWith(LocalizationAssetDir))
                {
                    collection = MigrateCollection(collection);
                }
            }

            if (collection == null)
            {
                EnsureLocalizationDir();
                collection = LocalizationEditorSettings.CreateStringTableCollection(CollectionName, LocalizationAssetDir, locales);
            }

            for (int i = 0; i < locales.Length; i++)
            {
                Locale locale = locales[i];
                if (locale == null || collection.GetTable(locale.Identifier) != null)
                {
                    continue;
                }

                collection.AddNewTable(locale.Identifier);
            }

            return collection;
        }

        /// <summary>
        /// Card 表目录与 GameText 表目录不一致时自动迁移（移动 .asset 文件）。
        /// zh-Hans 数据不会丢失（来自 CardData，Sync 后重建）；en 数据保留在 Card.csv，Import 后恢复。
        /// </summary>
        private static StringTableCollection MigrateCollection(StringTableCollection collection)
        {
            EnsureLocalizationDir();

            var assetPaths = new List<string>();

            foreach (var table in collection.StringTables)
            {
                string path = AssetDatabase.GetAssetPath(table);
                if (!string.IsNullOrEmpty(path))
                {
                    assetPaths.Add(path);
                }
            }

            if (collection.SharedData != null)
            {
                string path = AssetDatabase.GetAssetPath(collection.SharedData);
                if (!string.IsNullOrEmpty(path))
                {
                    assetPaths.Add(path);
                }
            }

            string collPath = AssetDatabase.GetAssetPath(collection);
            if (!string.IsNullOrEmpty(collPath))
            {
                assetPaths.Add(collPath);
            }

            foreach (string oldPath in assetPaths)
            {
                string fileName = Path.GetFileName(oldPath);
                string newPath = LocalizationAssetDir + "/" + fileName;
                string error = AssetDatabase.MoveAsset(oldPath, newPath);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"[CardLocalizationSync] Failed to move '{oldPath}': {error}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CardLocalizationSync] Migrated Card tables to {LocalizationAssetDir}.");
            return LocalizationEditorSettings.GetStringTableCollection(CollectionName);
        }

        private static void RemoveStaleEntries(StringTableCollection collection, HashSet<string> liveKeys)
        {
            if (collection == null || collection.SharedData == null)
            {
                return;
            }

            string[] staleKeys = collection.SharedData.Entries
                .Select(entry => entry.Key)
                .Where(key => !string.IsNullOrWhiteSpace(key) && !liveKeys.Contains(key))
                .ToArray();

            for (int i = 0; i < staleKeys.Length; i++)
            {
                collection.RemoveEntry(staleKeys[i]);
            }
        }

        private static StringTableEntry GetOrCreateEntry(StringTable table, string key)
        {
            StringTableEntry entry = table.GetEntry(key);
            return entry ?? table.AddEntry(key, string.Empty);
        }

        private static void UpsertEntry(StringTable table, string key, string value)
        {
            StringTableEntry entry = GetOrCreateEntry(table, key);
            string nextValue = value ?? string.Empty;
            if (!string.Equals(entry.Value, nextValue, StringComparison.Ordinal))
            {
                entry.Value = nextValue;
            }
        }

        private static void EnsureLocalizationDir()
        {
            if (AssetDatabase.IsValidFolder(LocalizationAssetDir))
            {
                return;
            }

            string[] parts = LocalizationAssetDir.Split('/');
            string currentPath = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }

                currentPath = nextPath;
            }
        }

        private static void WriteCsv(IReadOnlyList<CardTextRow> rows)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Key,zh-Hans,en");

            for (int i = 0; i < rows.Count; i++)
            {
                CardTextRow row = rows[i];
                builder.Append(EscapeCsv(row.Key));
                builder.Append(',');
                builder.Append(EscapeCsv(row.ZhHans));
                builder.Append(',');
                builder.Append(EscapeCsv(row.En));
                builder.AppendLine();
            }

            File.WriteAllText(CsvPath, builder.ToString(), new UTF8Encoding(true));
        }

        private static Dictionary<string, CardTextRow> ReadCsv()
        {
            string raw = File.ReadAllText(CsvPath, Encoding.UTF8);
            List<string[]> rows = ParseCsv(raw);
            if (rows.Count == 0)
            {
                return new Dictionary<string, CardTextRow>(StringComparer.Ordinal);
            }

            Dictionary<string, int> headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            string[] header = rows[0];
            for (int i = 0; i < header.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(header[i]))
                {
                    headerMap[header[i].Trim()] = i;
                }
            }

            Dictionary<string, CardTextRow> result = new Dictionary<string, CardTextRow>(StringComparer.Ordinal);
            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                string[] row = rows[rowIndex];
                string key = GetField(row, headerMap, "Key");
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                result[key] = new CardTextRow(0, string.Empty, key, GetField(row, headerMap, "zh-Hans"))
                {
                    En = GetField(row, headerMap, "en")
                };
            }

            return result;
        }

        private static string GetField(string[] row, Dictionary<string, int> headerMap, string name)
        {
            return headerMap.TryGetValue(name, out int index) && index >= 0 && index < row.Length
                ? row[index]
                : string.Empty;
        }

        private static List<string[]> ParseCsv(string content)
        {
            List<string[]> rows = new List<string[]>();
            List<string> currentRow = new List<string>();
            StringBuilder field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < content.Length; i++)
            {
                char ch = content[i];

                if (ch == '"')
                {
                    if (inQuotes && i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (!inQuotes && ch == ',')
                {
                    currentRow.Add(field.ToString());
                    field.Length = 0;
                    continue;
                }

                if (!inQuotes && (ch == '\n' || ch == '\r'))
                {
                    if (ch == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                    {
                        i++;
                    }

                    currentRow.Add(field.ToString());
                    field.Length = 0;
                    rows.Add(currentRow.ToArray());
                    currentRow = new List<string>();
                    continue;
                }

                field.Append(ch);
            }

            if (field.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(field.ToString());
                rows.Add(currentRow.ToArray());
            }

            if (rows.Count > 0 && rows[rows.Count - 1].Length == 1 && string.IsNullOrEmpty(rows[rows.Count - 1][0]))
            {
                rows.RemoveAt(rows.Count - 1);
            }

            return rows;
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        private sealed class CardTextRow
        {
            public CardTextRow(int cardId, string field, string key, string zhHans)
            {
                CardId = cardId;
                Field = field ?? string.Empty;
                Key = key ?? string.Empty;
                ZhHans = zhHans ?? string.Empty;
                En = string.Empty;
            }

            public int CardId { get; }
            public string Field { get; }
            public string Key { get; }
            public string ZhHans { get; }
            public string En { get; set; }
        }
    }
}
