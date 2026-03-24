using System.Collections.Generic;
using System.IO;
using BaoZuPo.Card;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;

namespace BaoZuPo.Editor
{
    /// <summary>
    /// Excel import tool for reading the card table and generating CardData assets.
    /// </summary>
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
        private const string Col_Wait = "waitTurns";
        private const string Col_Durability = "durability";
        private const string Col_PreEffect = "preEffect";
        private const string Col_InstantEffect = "instantEffect";
        private const string Col_SettleEffect = "settleEffect";
        private const string Col_DestroyEffect = "destroyEffect";

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

            int created = 0;
            int updated = 0;
            int skipped = 0;

            for (int rowIndex = DataStartRowIndex; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                IRow row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                int cardId = GetIntValue(row, columnMap, Col_CardId, -1);
                if (cardId < 0)
                {
                    continue;
                }

                string cardName = GetStringValue(row, columnMap, Col_CardName);
                if (string.IsNullOrEmpty(cardName))
                {
                    skipped++;
                    Debug.LogWarning($"[CardDataImporter] Row {rowIndex + 1}: ID={cardId} missing card name, skipped.");
                    continue;
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
                cardData.cardType = ParseCardType(GetStringValue(row, columnMap, Col_CardType));
                cardData.rarity = (CardRarity)GetIntValue(row, columnMap, Col_Rarity, 0);
                cardData.cost = GetIntValue(row, columnMap, Col_Cost, 0);
                cardData.baseRent = GetIntValue(row, columnMap, Col_BaseRent, 0);
                cardData.waitTurns = GetIntValue(row, columnMap, Col_Wait, 0);
                cardData.durability = GetIntValue(row, columnMap, Col_Durability, 0);
                cardData.preEffect = GetStringValue(row, columnMap, Col_PreEffect);
                cardData.instantEffect = GetStringValue(row, columnMap, Col_InstantEffect);
                cardData.settleEffect = GetStringValue(row, columnMap, Col_SettleEffect);
                cardData.destroyEffect = GetStringValue(row, columnMap, Col_DestroyEffect);

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

            Debug.Log($"[CardDataImporter] Done. Created {created}, updated {updated}, skipped {skipped}.");
        }

        private static string GetStringValue(IRow row, Dictionary<string, int> columnMap, string columnName)
        {
            if (!columnMap.TryGetValue(columnName, out int colIndex)) return "";

            ICell cell = row.GetCell(colIndex);
            if (cell == null) return "";

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue?.Trim() ?? "",
                CellType.Numeric => cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.ToString()?.Trim() ?? "",
                _ => ""
            };
        }

        private static int GetIntValue(IRow row, Dictionary<string, int> columnMap, string columnName, int defaultValue)
        {
            if (!columnMap.TryGetValue(columnName, out int colIndex)) return defaultValue;

            ICell cell = row.GetCell(colIndex);
            if (cell == null) return defaultValue;

            return cell.CellType switch
            {
                CellType.Numeric => (int)cell.NumericCellValue,
                CellType.String => int.TryParse(cell.StringCellValue, out int val) ? val : defaultValue,
                _ => defaultValue
            };
        }

        private static CardType ParseCardType(string typeString)
        {
            if (string.IsNullOrEmpty(typeString)) return CardType.Tenant;

            if (CardTypeMap.TryGetValue(typeString, out var cardType))
                return cardType;

            Debug.LogWarning($"[CardDataImporter] Unknown card type: {typeString}, defaulting to Tenant.");
            return CardType.Tenant;
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
