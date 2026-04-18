using System;
using System.Collections.Generic;
using System.IO;
using BaoZuPo.Card;
using Martian.RandomEvent;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;

namespace BaoZuPo.Editor
{
    public static class RandomEventDataImporter
    {
        private const string ExcelPath        = "Assets/Data/Excel/RandomEventData.xlsx";
        private const string EventOutputFolder   = "Assets/Resources/RandomEvents";
        private const string LibraryOutputFolder = "Assets/Resources/RandomEventLibraries";

        // Sheet 0: EventData columns
        private const string Col_EventId      = "eventId";
        private const string Col_Title        = "title";
        private const string Col_Description  = "description";
        private const string Col_Tags         = "tags";
        private const string Col_OptionId     = "optionId";
        private const string Col_OptionText   = "optionText";
        private const string Col_OptionTooltip= "optionTooltip";
        private const string Col_Effect       = "effect";
        private const string Col_NextEventId  = "nextEventId";

        // Sheet 1: Libraries columns
        private const string Col_LibraryId    = "libraryId";
        private const string Col_Weight       = "weight";

        private const int HeaderRow  = 1; // row index (0-based), row 2 in Excel
        private const int DataStart  = 3; // row index (0-based), row 4 in Excel

        [MenuItem("Tools/BaoZuPo/随机事件/导入随机事件数据")]
        public static void Import()
        {
            CardEffectRegistration.EnsureRegistered();

            if (!TryOpenWorkbook(out var workbook)) return;

            EnsureFolder(EventOutputFolder);
            EnsureFolder(LibraryOutputFolder);

            var events = ImportEvents(workbook);
            ImportLibraries(workbook, events);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RandomEventDataImporter] Done. Imported {events.Count} events.");
        }

        // ── Event sheet ──────────────────────────────────────────────────────

        private static Dictionary<string, RandomEventData> ImportEvents(IWorkbook workbook)
        {
            var sheet = workbook.GetSheetAt(0);
            if (sheet == null) throw new InvalidDataException("[RandomEventDataImporter] Sheet 0 (EventData) not found.");

            var colMap = BuildColumnMap(sheet.GetRow(HeaderRow));
            var result = new Dictionary<string, RandomEventData>();

            string currentEventId = null, currentTitle = null, currentDescription = null, currentTags = null;

            for (int r = DataStart; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null || IsRowEmpty(row)) continue;

                // 继承上一行的事件信息（留空=同上）
                var eventId = GetStr(row, colMap, Col_EventId);
                if (!string.IsNullOrWhiteSpace(eventId)) currentEventId = eventId;

                var title = GetStr(row, colMap, Col_Title);
                if (!string.IsNullOrWhiteSpace(title)) currentTitle = title;

                var desc = GetStr(row, colMap, Col_Description);
                if (!string.IsNullOrWhiteSpace(desc)) currentDescription = desc;

                var tags = GetStr(row, colMap, Col_Tags);
                if (!string.IsNullOrWhiteSpace(tags)) currentTags = tags;

                if (string.IsNullOrWhiteSpace(currentEventId))
                    throw new InvalidDataException($"[RandomEventDataImporter] Row {r + 1}: eventId is missing.");

                var optionId = GetStr(row, colMap, Col_OptionId);
                if (string.IsNullOrWhiteSpace(optionId))
                    throw new InvalidDataException($"[RandomEventDataImporter] Row {r + 1}: optionId is required.");

                var effect = GetStr(row, colMap, Col_Effect);
                if (!string.IsNullOrWhiteSpace(effect) && !CardEffectFactory.TryValidate(effect, out var err))
                    throw new InvalidDataException($"[RandomEventDataImporter] Row {r + 1}, option '{optionId}': invalid effect '{effect}'. {err}");

                if (!result.TryGetValue(currentEventId, out var data))
                {
                    string assetPath = $"{EventOutputFolder}/{currentEventId}.asset";
                    data = AssetDatabase.LoadAssetAtPath<RandomEventData>(assetPath)
                           ?? ScriptableObject.CreateInstance<RandomEventData>();

                    data.eventId        = currentEventId;
                    data.titleKey       = currentTitle ?? string.Empty;
                    data.descriptionKey = currentDescription ?? string.Empty;
                    data.tags           = string.IsNullOrWhiteSpace(currentTags)
                                          ? Array.Empty<string>()
                                          : currentTags.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    data.options        = new System.Collections.Generic.List<RandomEventOption>();

                    if (!AssetDatabase.Contains(data))
                        AssetDatabase.CreateAsset(data, assetPath);

                    result[currentEventId] = data;
                }

                data.options.Add(new RandomEventOption
                {
                    optionId       = optionId,
                    displayTextKey = GetStr(row, colMap, Col_OptionText),
                    tooltipKey     = GetStr(row, colMap, Col_OptionTooltip),
                    effect         = effect,
                    nextEventId    = GetStr(row, colMap, Col_NextEventId),
                });

                EditorUtility.SetDirty(data);
            }

            // validate: each event must have at least one option
            foreach (var kv in result)
                if (kv.Value.options.Count == 0)
                    throw new InvalidDataException($"[RandomEventDataImporter] Event '{kv.Key}' has no options.");

            return result;
        }

        // ── Library sheet ────────────────────────────────────────────────────

        private static void ImportLibraries(IWorkbook workbook, Dictionary<string, RandomEventData> events)
        {
            var sheet = workbook.GetSheetAt(1);
            if (sheet == null) throw new InvalidDataException("[RandomEventDataImporter] Sheet 1 (Libraries) not found.");

            var colMap = BuildColumnMap(sheet.GetRow(HeaderRow));
            var byLibrary = new Dictionary<string, List<RandomEventEntry>>();

            for (int r = DataStart; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null || IsRowEmpty(row)) continue;

                var libraryId = GetStr(row, colMap, Col_LibraryId);
                var eventId   = GetStr(row, colMap, Col_EventId);
                var weightStr = GetStr(row, colMap, Col_Weight);

                if (string.IsNullOrWhiteSpace(libraryId) || string.IsNullOrWhiteSpace(eventId)) continue;

                if (!events.TryGetValue(eventId, out var data))
                    throw new InvalidDataException($"[RandomEventDataImporter] Row {r + 1}: eventId '{eventId}' not found in imported events.");

                if (!int.TryParse(weightStr, out int weight) || weight < 1) weight = 1;

                if (!byLibrary.ContainsKey(libraryId)) byLibrary[libraryId] = new List<RandomEventEntry>();
                byLibrary[libraryId].Add(new RandomEventEntry { data = data, weight = weight });
            }

            foreach (var kv in byLibrary)
            {
                string assetPath = $"{LibraryOutputFolder}/{kv.Key}.asset";
                var lib = AssetDatabase.LoadAssetAtPath<RandomEventLibrary>(assetPath)
                          ?? ScriptableObject.CreateInstance<RandomEventLibrary>();

                lib.libraryId   = kv.Key;
                lib.displayName = kv.Key;
                lib.entries     = kv.Value;

                if (!AssetDatabase.Contains(lib))
                    AssetDatabase.CreateAsset(lib, assetPath);

                EditorUtility.SetDirty(lib);
                Debug.Log($"[RandomEventDataImporter] Library '{kv.Key}' synced with {kv.Value.Count} entries.");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static bool TryOpenWorkbook(out IWorkbook workbook)
        {
            workbook = null;
            string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, ExcelPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[RandomEventDataImporter] Excel not found: {fullPath}");
                return false;
            }
            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            workbook = new XSSFWorkbook(stream);
            return true;
        }

        private static Dictionary<string, int> BuildColumnMap(IRow headerRow)
        {
            var map = new Dictionary<string, int>();
            if (headerRow == null) return map;
            for (int c = headerRow.FirstCellNum; c < headerRow.LastCellNum; c++)
            {
                var cell = headerRow.GetCell(c);
                if (cell == null) continue;
                var text = cell.ToString().Trim();
                if (!string.IsNullOrEmpty(text)) map[text] = c;
            }
            return map;
        }

        private static string GetStr(IRow row, Dictionary<string, int> colMap, string col)
        {
            if (!colMap.TryGetValue(col, out int idx)) return string.Empty;
            var cell = row.GetCell(idx);
            if (cell == null) return string.Empty;
            return cell.CellType switch
            {
                CellType.String  => cell.StringCellValue?.Trim() ?? string.Empty,
                CellType.Numeric => cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.ToString()?.Trim() ?? string.Empty,
                _ => string.Empty,
            };
        }

        private static bool IsRowEmpty(IRow row)
        {
            for (int c = row.FirstCellNum; c < row.LastCellNum; c++)
            {
                var cell = row.GetCell(c);
                if (cell != null && !string.IsNullOrWhiteSpace(cell.ToString())) return false;
            }
            return true;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
