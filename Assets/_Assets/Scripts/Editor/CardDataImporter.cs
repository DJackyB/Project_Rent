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
    /// Imports card assets from Excel and rebuilds CardLibrary assets from the
    /// per-card `libraries` column. Library ids remain code-defined in
    /// CardSheetCatalog.
    /// </summary>
    public static class CardDataImporter
    {
        private const string ExcelRelativePath = "Assets/_Assets/Data/Excel/CardData.xlsx";
        private const string OutputFolder = "Assets/Resources/Cards";
        private const string LibraryOutputFolder = "Assets/Resources/CardLibraries";
        private const string GameConfigAssetPath = "Assets/_Assets/Data/Config/GameConfig.asset";
        private const int HeaderRowIndex = 1;
        private const int DataStartRowIndex = 3;
        private const int SheetIndex = 0;

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
        private const string Col_Libraries = "libraries";

        private static readonly char[] LibrarySeparators = { '|', ',', '\n', '\r' };

        private static readonly string[] RequiredColumns =
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
            Col_Libraries,
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

            if (!TryLoadExcelSheet(out var sheet, out var columnMap))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                CreateFolderRecursive(OutputFolder);
            }

            ValidateRequiredColumns(columnMap, RequiredColumns);
            var libraryCardIdsByLibraryId = BuildLibraryCardIdMap(sheet, columnMap);

            int created = 0;
            int updated = 0;
            var importedCardIds = new HashSet<int>();

            for (int rowIndex = DataStartRowIndex; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                IRow row = sheet.GetRow(rowIndex);
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

            SyncCardLibraries(libraryCardIdsByLibraryId);

            Debug.Log($"[CardDataImporter] Done. Created {created}, updated {updated}, synced Card localization, and refreshed Card libraries.");
        }

        [MenuItem("Tools/BaoZuPo/卡牌/同步卡库")]
        public static void SyncCardLibraries()
        {
            if (!TryLoadExcelSheet(out var sheet, out var columnMap))
            {
                return;
            }

            var libraryCardIdsByLibraryId = BuildLibraryCardIdMap(sheet, columnMap);
            SyncCardLibraries(libraryCardIdsByLibraryId);
        }

        private static void SyncCardLibraries(Dictionary<string, List<int>> libraryCardIdsByLibraryId)
        {
            if (!AssetDatabase.IsValidFolder(LibraryOutputFolder))
            {
                CreateFolderRecursive(LibraryOutputFolder);
            }

            var cardsById = LoadManagedCardsById();
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

                if (!libraryCardIdsByLibraryId.TryGetValue(spec.LibraryId, out var configuredCardIds))
                {
                    throw new InvalidDataException($"[CardDataImporter] Missing generated card list for library '{spec.LibraryId}'.");
                }

                library.libraryId = spec.LibraryId;
                library.displayName = spec.DisplayName;
                library.cards = ResolveLibraryCards(spec.LibraryId, configuredCardIds, cardsById);

                if (isNew)
                {
                    AssetDatabase.CreateAsset(library, assetPath);
                }
                else
                {
                    EditorUtility.SetDirty(library);
                }

                librariesById[spec.LibraryId] = library;
                Debug.Log($"[CardDataImporter] {(isNew ? "Created" : "Updated")} library '{spec.LibraryId}' with {library.cards.Count} entries.");
            }

            SyncGameConfig(librariesById);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool TryLoadExcelSheet(out ISheet sheet, out Dictionary<string, int> columnMap)
        {
            sheet = null;
            columnMap = null;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string excelPath = Path.Combine(projectRoot, ExcelRelativePath);

            if (!File.Exists(excelPath))
            {
                Debug.LogError($"[CardDataImporter] Excel file not found: {excelPath}");
                return false;
            }

            IWorkbook workbook;
            using (var stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                workbook = new XSSFWorkbook(stream);
            }

            sheet = workbook.GetSheetAt(SheetIndex);
            if (sheet == null)
            {
                Debug.LogError($"[CardDataImporter] Sheet {SheetIndex} not found.");
                return false;
            }

            IRow headerRow = sheet.GetRow(HeaderRowIndex);
            if (headerRow == null)
            {
                Debug.LogError($"[CardDataImporter] Header row {HeaderRowIndex + 1} is empty.");
                return false;
            }

            columnMap = BuildColumnMap(headerRow);
            Debug.Log($"[CardDataImporter] Found {columnMap.Count} columns: {string.Join(", ", columnMap.Keys)}");
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
                _ => throw new InvalidDataException($"[CardDataImporter] Row {rowIndex + 1}: column '{columnName}' is not a valid int."),
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

        private static Dictionary<string, List<int>> BuildLibraryCardIdMap(ISheet sheet, Dictionary<string, int> columnMap)
        {
            ValidateRequiredColumns(columnMap, Col_CardId, Col_Libraries);

            var librarySpecsById = BuildLibrarySpecsById();
            var libraryCardIdsByLibraryId = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            var autoIncludeLibraryIds = new List<string>();

            foreach (var spec in CardSheetCatalog.Libraries)
            {
                libraryCardIdsByLibraryId[spec.LibraryId] = new List<int>();

                if (spec.IncludeAllCards)
                {
                    autoIncludeLibraryIds.Add(spec.LibraryId);
                }
            }

            for (int rowIndex = DataStartRowIndex; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                IRow row = sheet.GetRow(rowIndex);
                if (row == null || IsRowEmpty(row))
                {
                    continue;
                }

                int cardId = GetRequiredIntValue(row, columnMap, Col_CardId, rowIndex);

                for (int i = 0; i < autoIncludeLibraryIds.Count; i++)
                {
                    libraryCardIdsByLibraryId[autoIncludeLibraryIds[i]].Add(cardId);
                }

                string configuredLibraries = GetStringValue(row, columnMap, Col_Libraries);
                var explicitLibraryIds = ParseExplicitLibraryIds(configuredLibraries, rowIndex, cardId, librarySpecsById);
                for (int i = 0; i < explicitLibraryIds.Count; i++)
                {
                    string libraryId = explicitLibraryIds[i];
                    libraryCardIdsByLibraryId[libraryId].Add(cardId);
                }
            }

            return libraryCardIdsByLibraryId;
        }

        private static Dictionary<string, CardSheetCatalog.LibraryRow> BuildLibrarySpecsById()
        {
            var librarySpecsById = new Dictionary<string, CardSheetCatalog.LibraryRow>(StringComparer.OrdinalIgnoreCase);

            foreach (var spec in CardSheetCatalog.Libraries)
            {
                if (librarySpecsById.ContainsKey(spec.LibraryId))
                {
                    throw new InvalidOperationException($"[CardDataImporter] Duplicate library definition '{spec.LibraryId}' in CardSheetCatalog.");
                }

                librarySpecsById[spec.LibraryId] = spec;
            }

            return librarySpecsById;
        }

        private static List<string> ParseExplicitLibraryIds(
            string configuredLibraries,
            int rowIndex,
            int cardId,
            Dictionary<string, CardSheetCatalog.LibraryRow> librarySpecsById)
        {
            var explicitLibraryIds = new List<string>();

            if (string.IsNullOrWhiteSpace(configuredLibraries))
            {
                return explicitLibraryIds;
            }

            string[] tokens = configuredLibraries.Split(LibrarySeparators, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawToken in tokens)
            {
                string trimmedToken = rawToken.Trim();
                if (string.IsNullOrWhiteSpace(trimmedToken))
                {
                    continue;
                }

                if (!librarySpecsById.TryGetValue(trimmedToken, out var spec))
                {
                    throw new InvalidDataException(
                        $"[CardDataImporter] Row {rowIndex + 1}, card {cardId}: unknown libraryId '{trimmedToken}' in '{Col_Libraries}'. Known ids: {GetKnownExplicitLibraryIds()}");
                }

                if (spec.IncludeAllCards)
                {
                    throw new InvalidDataException(
                        $"[CardDataImporter] Row {rowIndex + 1}, card {cardId}: library '{spec.LibraryId}' is auto-populated and should not be listed in '{Col_Libraries}'.");
                }

                // Repeated ids are preserved as extra weight within the same library.
                explicitLibraryIds.Add(spec.LibraryId);
            }

            return explicitLibraryIds;
        }

        private static string GetKnownExplicitLibraryIds()
        {
            var libraryIds = new List<string>();

            foreach (var spec in CardSheetCatalog.Libraries)
            {
                if (!spec.IncludeAllCards)
                {
                    libraryIds.Add(spec.LibraryId);
                }
            }

            return string.Join(", ", libraryIds);
        }

        private static List<CardData> ResolveLibraryCards(string libraryId, List<int> cardIds, Dictionary<int, CardData> cardsById)
        {
            var cards = new List<CardData>(cardIds.Count);

            foreach (int cardId in cardIds)
            {
                if (!cardsById.TryGetValue(cardId, out var card))
                {
                    throw new InvalidDataException($"[CardDataImporter] Library '{libraryId}' references missing cardId {cardId}. Import cards first.");
                }

                cards.Add(card);
            }

            return cards;
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

            Debug.Log("[CardDataImporter] Synced GameConfig draw libraries to library ids 0 / 1 / 2.");
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
