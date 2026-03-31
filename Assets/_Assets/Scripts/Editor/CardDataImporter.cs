using System.Collections.Generic;
using System.IO;
using BaoZuPo.Card;
using BaoZuPo.GameFlow;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;

namespace BaoZuPo.Editor
{
    public static class CardDataImporter
    {
        private const string ExcelRelativePath = "Assets/_Assets/Data/Excel/CardData.xlsx";
        private const string OutputFolder = "Assets/Resources/Cards";
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

        [MenuItem("Tools/BaoZuPo/Import Card Data")]
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

            for (int rowIndex = DataStartRowIndex; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                IRow row = sheet.GetRow(rowIndex);
                if (row == null || IsRowEmpty(row))
                {
                    continue;
                }

                int cardId = GetRequiredIntValue(row, columnMap, Col_CardId, rowIndex);

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
                if (!string.IsNullOrEmpty(artPath))
                {
                    cardData.cardArt = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
                }

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

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CardDataImporter] Done. Created {created}, updated {updated}.");
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
    }
}
