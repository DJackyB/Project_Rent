using System.Collections.Generic;
using System.IO;
using BaoZuPo.Card;
using BaoZuPo.GameFlow;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;
using BaoZuPo.Core;

namespace BaoZuPo.Editor
{
    /// <summary>
    /// 卡牌数据导表工具。
    /// 从 Excel 文件读取卡牌定义，生成游戏资产（CardData.asset 和 CardLibrary.asset）。
    ///
    /// 使用流程：
    /// 1. 在 Excel 中编辑卡牌数据 (Assets/_Assets/Data/Excel/CardData.xlsx)
    /// 2. 在 Editor 中运行菜单项：Tools → BaoZuPo → Import Card Data
    /// 3. 工具读取 Excel，验证数据，生成资产到 Assets/Resources/Cards/ 和 Assets/Resources/CardLibraries/
    /// 4. 验证失败时中止，输出错误信息
    /// 5. 验证成功后，资产立即可用（ResourceManager.Load<CardData>(...) 可访问）
    ///
    /// 验证项目：
    /// - Excel 格式：表头行、英文列名、数据类型声明
    /// - 卡牌库配置：firstTurnDrawLibrary、normalTurnDrawLibrary、rewardLibrary 必须存在且非空
    /// - 卡牌效果：每张卡的 preEffect、instantEffect、settleEffect、destroyEffect 必须能被 CardEffectFactory 解析
    /// - 卡牌目标：每张卡的 targetKind 与 cardType 必须匹配（例如 Tenant/Equipment 需要 Room 目标）
    /// </summary>
    public static class CardDataImporter
    {
        private const string ExcelRelativePath = "Assets/_Assets/Data/Excel/CardData.xlsx";
        private const string OutputFolder = "Assets/Resources/Cards";
        private const string LibraryOutputFolder = "Assets/Resources/CardLibraries";
        private const string GameConfigAssetPath = "Assets/_Assets/Data/Config/GameConfig.asset";
        private const int HeaderRowIndex = 1;      // 英文列名行
        private const int DataStartRowIndex = 3;   // 数据开始行（跳过中文名、英文名、类型行）
        private const int SheetIndex = 0;          // 工作表索引

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
        };

        private static readonly Dictionary<string, CardType> CardTypeMap = new()
        {
            { "Card_Tenant", CardType.Tenant },
            { "Card_Equipt", CardType.Equipment },
            { "Card_Equipment", CardType.Equipment },
            { "Card_Event", CardType.Event },
            { "Card_Contract", CardType.Contract },
        };

        /// <summary>
        /// 导入卡牌数据的主入口（菜单项）。
        /// 流程：打开 Excel → 解析 → 验证 → 生成资产 → 刷新 AssetDatabase
        /// 任何验证失败都会中止并输出错误信息。
        /// </summary>
        [MenuItem("Tools/BaoZuPo/Cards/Import Card Data")]
        public static void Import()
        {
            CardEffectRegistration.EnsureRegistered();

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string excelPath = Path.Combine(projectRoot, ExcelRelativePath);

            if (!File.Exists(excelPath))
            {
                Debug.LogError($"[CardDataImporter] Excel file not found: {excelPath}");
                return;
            }

            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                CreateFolderRecursive(OutputFolder);
            }

            IWorkbook workbook;
            using (var stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                workbook = new XSSFWorkbook(stream);
            }

            ISheet sheet = workbook.GetSheetAt(SheetIndex);
            if (sheet == null)
            {
                Debug.LogError($"[CardDataImporter] Sheet {SheetIndex} not found.");
                return;
            }

            IRow headerRow = sheet.GetRow(HeaderRowIndex);
            if (headerRow == null)
            {
                Debug.LogError($"[CardDataImporter] Header row {HeaderRowIndex + 1} is empty.");
                return;
            }

            var columnMap = new Dictionary<string, int>();
            for (int col = headerRow.FirstCellNum; col < headerRow.LastCellNum; col++)
            {
                ICell cell = headerRow.GetCell(col);
                if (cell != null)
                {
                    string headerText = cell.ToString().Trim();
                    if (!string.IsNullOrEmpty(headerText))
                    {
                        columnMap[headerText] = col;
                    }
                }
            }

            Debug.Log($"[CardDataImporter] Found {columnMap.Count} columns: {string.Join(", ", columnMap.Keys)}");
            ValidateRequiredColumns(columnMap);

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
                cardData.targetKind = ParseTargetKind(
                    GetStringValue(row, columnMap, Col_TargetKind),
                    rowIndex,
                    cardId);

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
            SyncCardLibraries();

            Debug.Log($"[CardDataImporter] Done. Created {created}, updated {updated}, synced Card localization, and refreshed Card libraries.");
        }

        [MenuItem("Tools/BaoZuPo/Cards/Sync Card Libraries")]
        public static void SyncCardLibraries()
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

                library.libraryId = spec.LibraryId;
                library.displayName = spec.DisplayName;
                library.cards = ResolveLibraryCards(spec, cardsById);

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

        private static string GetStringValue(IRow row, Dictionary<string, int> columnMap, string columnName)
        {
            if (!columnMap.TryGetValue(columnName, out int colIndex))
            {
                return "";
            }

            ICell cell = row.GetCell(colIndex);
            if (cell == null)
            {
                return "";
            }

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue?.Trim() ?? "",
                CellType.Numeric => cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.ToString()?.Trim() ?? "",
                _ => ""
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
                CellType.String => int.TryParse(cell.StringCellValue, out int val)
                    ? val
                    : throw new InvalidDataException($"[CardDataImporter] Row {rowIndex + 1}: column '{columnName}' is not a valid int."),
                _ => throw new InvalidDataException($"[CardDataImporter] Row {rowIndex + 1}: column '{columnName}' is not a valid int.")
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

            CardPlayTargetKind parsedTarget;
            if (System.Enum.TryParse(configuredTarget, true, out CardPlayTargetKind enumTarget))
            {
                parsedTarget = enumTarget;
            }
            else if (int.TryParse(configuredTarget, out int numericTarget)
                && System.Enum.IsDefined(typeof(CardPlayTargetKind), numericTarget))
            {
                parsedTarget = (CardPlayTargetKind)numericTarget;
            }
            else
            {
                throw new InvalidDataException(
                    $"[CardDataImporter] Row {rowIndex + 1}, card {cardId}: invalid targetKind '{configuredTarget}'.");
            }

            return parsedTarget;
        }

        private static void ValidateConfiguredTarget(int rowIndex, int cardId, CardData cardData)
        {
            if (CardTargeting.TryValidateConfiguredTargetKind(cardData, out var warning))
            {
                return;
            }

            throw new InvalidDataException(
                $"[CardDataImporter] Row {rowIndex + 1}, card {cardId}: {warning}");
        }

        private static void ValidateRequiredColumns(Dictionary<string, int> columnMap)
        {
            foreach (string requiredColumn in RequiredColumns)
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

        private static List<CardData> ResolveLibraryCards(CardSheetCatalog.LibraryRow spec, Dictionary<int, CardData> cardsById)
        {
            var cards = new List<CardData>(spec.CardIds.Length);
            foreach (int cardId in spec.CardIds)
            {
                if (!cardsById.TryGetValue(cardId, out var card))
                {
                    throw new InvalidDataException($"[CardDataImporter] Library '{spec.LibraryId}' references missing cardId {cardId}. Import cards first.");
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

            gameConfig.firstTurnDrawLibrary = librariesById["FirstTurnPool"];
            gameConfig.normalTurnDrawLibrary = librariesById["NormalTurnPool"];
            gameConfig.rewardLibrary = librariesById["RewardPool"];
            EditorUtility.SetDirty(gameConfig);

            Debug.Log("[CardDataImporter] Synced GameConfig draw libraries to FirstTurnPool / NormalTurnPool / RewardPool.");
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
