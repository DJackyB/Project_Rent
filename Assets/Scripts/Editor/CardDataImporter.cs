using System;
using System.Collections.Generic;
using System.IO;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.GameFlow;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;

namespace BaoZuPo.Editor
{
    /// <summary>
    /// Imports card assets from the CardData sheet (Sheet 0) and syncs CardLibrary
    /// assets from the CardLibrary sheet (Sheet 1). Library membership and quantity
    /// are defined entirely in Sheet 1 — the card sheet no longer has a libraries column.
    /// </summary>
    public static class CardDataImporter
    {
        private const string ExcelRelativePath = "Assets/Data/Excel/CardData.xlsx";
        private const string OutputFolder = "Assets/Resources/Cards";
        private const string LibraryOutputFolder = "Assets/Resources/CardLibraries";
        private const string GameConfigAssetPath = "Assets/Data/Config/GameConfig.asset";
        private const int HeaderRowIndex = 1;
        private const int DataStartRowIndex = 3;

        // ── Card sheet (Sheet 0) columns ──────────────────────────────────────
        private const string Col_CardId = "cardId";
        private const string Col_CardName = "cardName";
        private const string Col_Description = "description";
        private const string Col_CardType = "cardType";
        private const string Col_Rarity = "rarity";
        private const string Col_ArtPath = "cardArt";
        private const string Col_Cost = "cost";
        private const string Col_BaseRent = "baseRent";
        private const string Col_TargetKind = "targetKind";
        private const string Col_Wait = "waitTurns";
        private const string Col_Durability = "durability";
        private const string Col_PreEffect = "preEffect";
        private const string Col_InstantEffect = "instantEffect";
        private const string Col_SettleEffect = "settleEffect";
        private const string Col_DestroyEffect = "destroyEffect";

        private static readonly string[] CardSheetRequiredColumns =
        {
            Col_CardId,
            Col_CardName,
            Col_Description,
            Col_CardType,
            Col_Rarity,
            Col_ArtPath,
            Col_Cost,
            Col_BaseRent,
            Col_TargetKind,
            Col_Wait,
            Col_Durability,
            Col_PreEffect,
            Col_InstantEffect,
            Col_SettleEffect,
            Col_DestroyEffect,
        };

        // ── Library sheet (Sheet 1) columns ───────────────────────────────────
        private const string Col_LibraryId = "libraryId";
        private const string Col_Quantity = "quantity";
        // Col_CardId is shared

        private static readonly string[] LibrarySheetRequiredColumns =
        {
            Col_LibraryId,
            Col_CardId,
            Col_Quantity,
        };

        private static readonly Dictionary<string, CardType> CardTypeMap = new()
        {
            { "Card_Tenant", CardType.Tenant },
            { "Card_Equipt", CardType.Equipment },
            { "Card_Equipment", CardType.Equipment },
            { "Card_Event", CardType.Event },
            { "Card_Contract", CardType.Contract },
        };

        [MenuItem("Tools/BaoZuPo/卡牌/导入卡牌数据")]
        public static void Import()
        {
            CardEffectRegistration.EnsureRegistered();

            if (!TryOpenWorkbook(out var workbook))
            {
                return;
            }

            if (!TryGetSheetAndColumnMap(workbook, CardSheetCatalog.CardSheetIndex, out var cardSheet, out var columnMap))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                CreateFolderRecursive(OutputFolder);
            }

            ValidateRequiredColumns(columnMap, CardSheetRequiredColumns);

            int created = 0;
            int updated = 0;
            var importedCardIds = new HashSet<int>();

            for (int rowIndex = DataStartRowIndex; rowIndex <= cardSheet.LastRowNum; rowIndex++)
            {
                IRow row = cardSheet.GetRow(rowIndex);
                if (row == null || IsRowEmpty(row))
                {
                    continue;
                }

                int cardId = GetRequiredIntValue(row, columnMap, Col_CardId, rowIndex);
                importedCardIds.Add(cardId);

                string cardName = GetStringValue(row, columnMap, Col_CardName);
                if (string.IsNullOrWhiteSpace(cardName))
                {
                    throw new InvalidDataException($"[CardDataImporter] Row {rowIndex + 1}, card {cardId}: cardName is required.");
                }

                string assetPath = $"{OutputFolder}/Card_{cardId}.asset";
                CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
                bool isNew = cardData == null;

                if (isNew)
                {
                    cardData = ScriptableObject.CreateInstance<CardData>();
                }

                cardData.cardId = cardId;
                cardData.cardName = cardName;
                cardData.description = GetStringValue(row, columnMap, Col_Description);
                cardData.cardType = ParseCardType(GetStringValue(row, columnMap, Col_CardType), rowIndex, cardId);
                cardData.rarity = (CardRarity)GetRequiredIntValue(row, columnMap, Col_Rarity, rowIndex);
                cardData.cost = GetRequiredIntValue(row, columnMap, Col_Cost, rowIndex);
                cardData.baseRent = GetRequiredIntValue(row, columnMap, Col_BaseRent, rowIndex);
                cardData.waitTurns = GetRequiredIntValue(row, columnMap, Col_Wait, rowIndex);
                cardData.durability = GetRequiredIntValue(row, columnMap, Col_Durability, rowIndex);
                cardData.preEffect = GetStringValue(row, columnMap, Col_PreEffect);
                cardData.instantEffect = GetStringValue(row, columnMap, Col_InstantEffect);
                cardData.settleEffect = GetStringValue(row, columnMap, Col_SettleEffect);
                cardData.destroyEffect = GetStringValue(row, columnMap, Col_DestroyEffect);
                cardData.targetKind = ParseTargetKind(GetStringValue(row, columnMap, Col_TargetKind), rowIndex, cardId);

                ValidateEffectField(rowIndex, cardId, Col_PreEffect, cardData.preEffect);
                ValidateEffectField(rowIndex, cardId, Col_InstantEffect, cardData.instantEffect);
                ValidateEffectField(rowIndex, cardId, Col_SettleEffect, cardData.settleEffect);
                ValidateEffectField(rowIndex, cardId, Col_DestroyEffect, cardData.destroyEffect);
                ValidateConfiguredTarget(rowIndex, cardId, cardData);

                string artPath = GetStringValue(row, columnMap, Col_ArtPath);
                cardData.cardArt = string.IsNullOrWhiteSpace(artPath)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<Sprite>(artPath);

                if (isNew)
                {
                    AssetDatabase.CreateAsset(cardData, assetPath);
                    created++;
                }
                else
                {
                    EditorUtility.SetDirty(cardData);
                    updated++;
                }

                Debug.Log($"[CardDataImporter] {(isNew ? "Created" : "Updated")} card: [{cardId}] {cardName}");
            }

            DeleteStaleCardAssets(importedCardIds);
            CardLocalizationSyncUtility.SyncCardTablesFromCardData(exportCsv: true, logSummary: false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var cardsById = LoadManagedCardsById();
            var entriesByLibraryId = BuildLibraryEntriesFromSheet(workbook, cardsById);
            SyncCardLibraries(entriesByLibraryId);

            Debug.Log($"[CardDataImporter] Done. Created {created}, updated {updated}, synced Card localization, and refreshed Card libraries.");
        }

        [MenuItem("Tools/BaoZuPo/卡牌/同步卡库")]
        public static void SyncCardLibraries()
        {
            if (!TryOpenWorkbook(out var workbook))
            {
                return;
            }

            var cardsById = LoadManagedCardsById();
            var entriesByLibraryId = BuildLibraryEntriesFromSheet(workbook, cardsById);
            SyncCardLibraries(entriesByLibraryId);
        }

        // ── 内部实现 ──────────────────────────────────────────────────────────

        private static void SyncCardLibraries(Dictionary<string, List<CardLibraryEntry>> entriesByLibraryId)
        {
            if (!AssetDatabase.IsValidFolder(LibraryOutputFolder))
            {
                CreateFolderRecursive(LibraryOutputFolder);
            }

            var librariesById = new Dictionary<string, CardLibrary>();

            foreach (var spec in CardSheetCatalog.Libraries)
            {
                string assetPath = $"{LibraryOutputFolder}/{spec.AssetName}.asset";
                var library = AssetDatabase.LoadAssetAtPath<CardLibrary>(assetPath);
                bool isNew = library == null;

                if (isNew)
                {
                    library = ScriptableObject.CreateInstance<CardLibrary>();
                }

                if (!entriesByLibraryId.TryGetValue(spec.LibraryId, out var entries))
                {
                    throw new InvalidDataException($"[CardDataImporter] Missing entries for library '{spec.LibraryId}'.");
                }

                library.libraryId = spec.LibraryId;
                library.displayName = spec.DisplayName;
                library.entries = entries;

                if (isNew)
                {
                    AssetDatabase.CreateAsset(library, assetPath);
                }
                else
                {
                    EditorUtility.SetDirty(library);
                }

                librariesById[spec.LibraryId] = library;
                Debug.Log($"[CardDataImporter] {(isNew ? "Created" : "Updated")} library '{spec.LibraryId}' with {library.entries.Count} entries.");
            }

            SyncGameConfig(librariesById);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 从 Sheet 1（CardLibrary sheet）读取卡牌库条目，按 libraryId 分组返回。
        /// IncludeAllCards 的库不从 Sheet 1 读取，而是自动包含所有已导入的卡牌（quantity=1）。
        /// </summary>
        private static Dictionary<string, List<CardLibraryEntry>> BuildLibraryEntriesFromSheet(
            IWorkbook workbook,
            Dictionary<int, CardData> cardsById)
        {
            var result = new Dictionary<string, List<CardLibraryEntry>>();
            var librarySpecsById = new Dictionary<string, CardSheetCatalog.LibraryRow>(StringComparer.OrdinalIgnoreCase);

            foreach (var spec in CardSheetCatalog.Libraries)
            {
                result[spec.LibraryId] = new List<CardLibraryEntry>();
                librarySpecsById[spec.LibraryId] = spec;
            }

            if (!TryGetSheetAndColumnMap(workbook, CardSheetCatalog.LibrarySheetIndex, out var sheet, out var columnMap))
            {
                throw new InvalidDataException("[CardDataImporter] CardLibrary sheet (Sheet 1) not found in Excel file.");
            }

            ValidateRequiredColumns(columnMap, LibrarySheetRequiredColumns);

            for (int rowIndex = DataStartRowIndex; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                IRow row = sheet.GetRow(rowIndex);
                if (row == null || IsRowEmpty(row))
                {
                    continue;
                }

                string libraryIdStr = GetStringValue(row, columnMap, Col_LibraryId);
                if (string.IsNullOrWhiteSpace(libraryIdStr))
                {
                    continue;
                }

                if (!librarySpecsById.TryGetValue(libraryIdStr, out var spec))
                {
                    throw new InvalidDataException(
                        $"[CardDataImporter] Row {rowIndex + 1}: unknown libraryId '{libraryIdStr}'. Known ids: {string.Join(", ", librarySpecsById.Keys)}");
                }

                if (spec.IncludeAllCards)
                {
                    throw new InvalidDataException(
                        $"[CardDataImporter] Row {rowIndex + 1}: library '{spec.LibraryId}' is auto-populated (IncludeAllCards=true) and must not appear in the CardLibrary sheet.");
                }

                int cardId = GetRequiredIntValue(row, columnMap, Col_CardId, rowIndex);
                int quantity = GetRequiredIntValue(row, columnMap, Col_Quantity, rowIndex);
                if (quantity <= 0)
                {
                    throw new InvalidDataException(
                        $"[CardDataImporter] Row {rowIndex + 1}: libraryId '{libraryIdStr}' cardId {cardId} has invalid quantity {quantity}. Quantity must be greater than 0.");
                }

                if (!cardsById.TryGetValue(cardId, out var cardData))
                {
                    throw new InvalidDataException(
                        $"[CardDataImporter] Row {rowIndex + 1}: cardId {cardId} not found in imported cards. Import cards first.");
                }

                result[libraryIdStr].Add(new CardLibraryEntry { card = cardData, quantity = quantity });
            }

            // IncludeAllCards 库自动填充全部已导入卡牌
            foreach (var spec in CardSheetCatalog.Libraries)
            {
                if (!spec.IncludeAllCards)
                {
                    continue;
                }

                foreach (var kvp in cardsById)
                {
                    result[spec.LibraryId].Add(new CardLibraryEntry { card = kvp.Value, quantity = 1 });
                }
            }

            return result;
        }

        private static bool TryOpenWorkbook(out IWorkbook workbook)
        {
            workbook = null;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string excelPath = Path.Combine(projectRoot, ExcelRelativePath);

            if (!File.Exists(excelPath))
            {
                Debug.LogError($"[CardDataImporter] Excel file not found: {excelPath}");
                return false;
            }

            using (var stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                workbook = new XSSFWorkbook(stream);
            }

            return true;
        }

        private static bool TryGetSheetAndColumnMap(IWorkbook workbook, int sheetIndex, out ISheet sheet, out Dictionary<string, int> columnMap)
        {
            sheet = null;
            columnMap = null;

            sheet = workbook.GetSheetAt(sheetIndex);
            if (sheet == null)
            {
                Debug.LogError($"[CardDataImporter] Sheet {sheetIndex} not found.");
                return false;
            }

            IRow headerRow = sheet.GetRow(HeaderRowIndex);
            if (headerRow == null)
            {
                Debug.LogError($"[CardDataImporter] Header row {HeaderRowIndex + 1} is empty in sheet {sheetIndex} ('{sheet.SheetName}').");
                return false;
            }

            columnMap = BuildColumnMap(headerRow);
            Debug.Log($"[CardDataImporter] Sheet '{sheet.SheetName}': found {columnMap.Count} columns: {string.Join(", ", columnMap.Keys)}");
            return true;
        }

        private static string GetStringValue(IRow row, Dictionary<string, int> columnMap, string columnName)
        {
            if (!columnMap.TryGetValue(columnName, out int colIndex))
            {
                return string.Empty;
            }

            ICell cell = row.GetCell(colIndex);
            if (cell == null)
            {
                return string.Empty;
            }

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
                CellType.Numeric => cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.ToString()?.Trim() ?? string.Empty,
                _ => string.Empty,
            };
        }

        private static int GetRequiredIntValue(IRow row, Dictionary<string, int> columnMap, string columnName, int rowIndex)
        {
            if (!columnMap.TryGetValue(columnName, out int colIndex))
            {
                throw new InvalidDataException($"[CardDataImporter] Row {rowIndex + 1}: missing required column '{columnName}'.");
            }

            ICell cell = row.GetCell(colIndex);
            if (cell == null)
            {
                throw new InvalidDataException($"[CardDataImporter] Row {rowIndex + 1}: column '{columnName}' is empty.");
            }

            return cell.CellType switch
            {
                CellType.Numeric => (int)cell.NumericCellValue,
                CellType.String => int.TryParse(cell.StringCellValue, out int value)
                    ? value
                    : throw new InvalidDataException($"[CardDataImporter] Row {rowIndex + 1}: column '{columnName}' is not a valid int."),
                _ => throw new InvalidDataException($"[CardDataImporter] Row {rowIndex + 1}: column '{columnName}' has unexpected cell type {cell.CellType}."),
            };
        }

        private static CardType ParseCardType(string typeString, int rowIndex, int cardId)
        {
            if (string.IsNullOrWhiteSpace(typeString))
            {
                throw new InvalidDataException($"[CardDataImporter] Row {rowIndex + 1}, card {cardId}: cardType is required.");
            }

            if (CardTypeMap.TryGetValue(typeString, out var cardType))
            {
                return cardType;
            }

            throw new InvalidDataException($"[CardDataImporter] Row {rowIndex + 1}, card {cardId}: unknown cardType '{typeString}'.");
        }

        private static void ValidateEffectField(int rowIndex, int cardId, string fieldName, string effectString)
        {
            if (CardEffectFactory.TryValidate(effectString, out var error))
            {
                return;
            }

            throw new InvalidDataException(
                $"[CardDataImporter] Row {rowIndex + 1}, card {cardId}, field '{fieldName}' is invalid: {effectString}. {error}");
        }

        private static CardPlayTargetKind ParseTargetKind(string configuredTarget, int rowIndex, int cardId)
        {
            if (string.IsNullOrWhiteSpace(configuredTarget))
            {
                throw new InvalidDataException($"[CardDataImporter] Row {rowIndex + 1}, card {cardId}: targetKind is required.");
            }

            if (Enum.TryParse(configuredTarget, true, out CardPlayTargetKind enumTarget))
            {
                return enumTarget;
            }

            if (int.TryParse(configuredTarget, out int numericTarget)
                && Enum.IsDefined(typeof(CardPlayTargetKind), numericTarget))
            {
                return (CardPlayTargetKind)numericTarget;
            }

            throw new InvalidDataException(
                $"[CardDataImporter] Row {rowIndex + 1}, card {cardId}: invalid targetKind '{configuredTarget}'.");
        }

        private static void ValidateConfiguredTarget(int rowIndex, int cardId, CardData cardData)
        {
            if (CardTargeting.TryValidateConfiguredTargetKind(cardData, out var warning))
            {
                return;
            }

            throw new InvalidDataException($"[CardDataImporter] Row {rowIndex + 1}, card {cardId}: {warning}");
        }

        private static void ValidateRequiredColumns(Dictionary<string, int> columnMap, params string[] requiredColumns)
        {
            foreach (string requiredColumn in requiredColumns)
            {
                if (!columnMap.ContainsKey(requiredColumn))
                {
                    throw new InvalidDataException($"[CardDataImporter] Missing required column '{requiredColumn}'.");
                }
            }
        }

        private static bool IsRowEmpty(IRow row)
        {
            if (row == null)
            {
                return true;
            }

            for (int col = row.FirstCellNum; col < row.LastCellNum; col++)
            {
                ICell cell = row.GetCell(col);
                if (cell != null && !string.IsNullOrWhiteSpace(cell.ToString()))
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<string, int> BuildColumnMap(IRow headerRow)
        {
            var columnMap = new Dictionary<string, int>();

            for (int col = headerRow.FirstCellNum; col < headerRow.LastCellNum; col++)
            {
                ICell cell = headerRow.GetCell(col);
                if (cell == null)
                {
                    continue;
                }

                string headerText = cell.ToString().Trim();
                if (!string.IsNullOrEmpty(headerText))
                {
                    columnMap[headerText] = col;
                }
            }

            return columnMap;
        }

        private static void CreateFolderRecursive(string folderPath)
        {
            string[] parts = folderPath.Split('/');
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

        private static Dictionary<int, CardData> LoadManagedCardsById()
        {
            var cardsById = new Dictionary<int, CardData>();
            string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { OutputFolder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card == null)
                {
                    continue;
                }

                cardsById[card.cardId] = card;
            }

            return cardsById;
        }

        private static void SyncGameConfig(Dictionary<string, CardLibrary> librariesById)
        {
            var gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigAssetPath);
            if (gameConfig == null)
            {
                Debug.LogWarning($"[CardDataImporter] GameConfig not found at '{GameConfigAssetPath}', skipping config sync.");
                return;
            }

            gameConfig.firstTurnDrawLibrary = librariesById[CardSheetCatalog.FirstTurnLibraryId];
            gameConfig.normalTurnDrawLibrary = librariesById[CardSheetCatalog.NormalTurnLibraryId];
            gameConfig.rewardLibrary = librariesById[CardSheetCatalog.RewardLibraryId];
            EditorUtility.SetDirty(gameConfig);

            Debug.Log("[CardDataImporter] Synced GameConfig draw libraries.");
        }

        private static void DeleteStaleCardAssets(HashSet<int> importedCardIds)
        {
            string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { OutputFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card == null)
                {
                    continue;
                }

                if (importedCardIds.Contains(card.cardId))
                {
                    continue;
                }

                Debug.Log($"[CardDataImporter] Deleting stale card asset: {path}");
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
