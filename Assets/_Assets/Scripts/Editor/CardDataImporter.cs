using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using BaoZuPo.Card;

namespace BaoZuPo.Editor
{
    /// <summary>
    /// Excel 导入工具：读取 Doc/包租婆.xlsx 的卡牌配置表，生成 CardData ScriptableObject。
    /// 使用方式：Unity 菜单 → Tools → 包租婆 → 导入卡牌表
    /// </summary>
    public static class CardDataImporter
    {
        // Excel 文件相对于项目根目录的路径（独立的卡牌配置表）
        private const string ExcelRelativePath = "Assets/_Assets/Data/Excel/CardData.xlsx";
        // 生成的 CardData 资产存放目录（相对于 Assets）
        private const string OutputFolder = "Assets/Resources/Cards";

        // 表头所在行（0-based），现在是第2行（英文字段）
        private const int HeaderRowIndex = 1;

        // 数据开始行（0-based），现在是第4行（第1行中文，第2行英文，第3行类型，第4行数据）
        private const int DataStartRowIndex = 3;

        // Sheet 页索引
        private const int SheetIndex = 0;

        // ---------- 表头英文名 → CardData 字段的映射 ----------
        // 这些名字必须和 Excel 第二行保持一致
        private const string Col_CardId = "cardId";
        private const string Col_CardName = "cardName";
        private const string Col_Description = "description";
        private const string Col_CardType = "cardType";
        private const string Col_Rarity = "rarity";
        private const string Col_ArtPath = "cardArt";
        private const string Col_Cost = "cost";
        private const string Col_Wait = "waitTurns";
        private const string Col_Durability = "durability";
        private const string Col_PreEffect = "preEffect";
        private const string Col_InstantEffect = "instantEffect";
        private const string Col_SettleEffect = "settleEffect";
        private const string Col_DestroyEffect = "destroyEffect";

        // 卡牌类型字符串 → 枚举映射
        private static readonly Dictionary<string, CardType> CardTypeMap = new()
        {
            { "Card_Tenant", CardType.Tenant },
            { "Card_Equipt", CardType.Equipment },
            { "Card_Equipment", CardType.Equipment },
            { "Card_Event", CardType.Event },
        };

        [MenuItem("Tools/包租婆/导入卡牌表")]
        public static void Import()
        {
            // 1. 定位 Excel 文件
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string excelPath = Path.Combine(projectRoot, ExcelRelativePath);

            if (!File.Exists(excelPath))
            {
                Debug.LogError($"[导表] 找不到 Excel 文件: {excelPath}");
                return;
            }

            // 确保输出目录存在
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                CreateFolderRecursive(OutputFolder);
            }

            // 2. 用 NPOI 打开 Excel
            IWorkbook workbook;
            using (var stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                workbook = new XSSFWorkbook(stream);
            }

            ISheet sheet = workbook.GetSheetAt(SheetIndex);
            if (sheet == null)
            {
                Debug.LogError($"[导表] 找不到 Sheet（索引 {SheetIndex}）");
                return;
            }

            // 3. 读取表头，建立列名 → 列索引映射
            IRow headerRow = sheet.GetRow(HeaderRowIndex);
            if (headerRow == null)
            {
                Debug.LogError($"[导表] 表头行（第 {HeaderRowIndex + 1} 行）为空");
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

            Debug.Log($"[导表] 识别到 {columnMap.Count} 个列: {string.Join(", ", columnMap.Keys)}");

            // 4. 逐行读取数据
            int created = 0;
            int updated = 0;
            int skipped = 0;

            for (int rowIndex = DataStartRowIndex; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                IRow row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                // 读取卡牌ID（必填字段，没有则跳过此行）
                int cardId = GetIntValue(row, columnMap, Col_CardId, -1);
                if (cardId < 0)
                {
                    continue; // 空行或无效行，静默跳过
                }

                string cardName = GetStringValue(row, columnMap, Col_CardName);
                if (string.IsNullOrEmpty(cardName))
                {
                    skipped++;
                    Debug.LogWarning($"[导表] 第 {rowIndex + 1} 行：ID={cardId} 缺少卡牌名称，已跳过");
                    continue;
                }

                // 查找或创建 CardData 资产
                string assetPath = $"{OutputFolder}/Card_{cardId}.asset";
                CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
                bool isNew = (cardData == null);

                if (isNew)
                {
                    cardData = ScriptableObject.CreateInstance<CardData>();
                }

                // 填充数据
                cardData.cardId = cardId;
                cardData.cardName = cardName;
                cardData.description = GetStringValue(row, columnMap, Col_Description);
                cardData.cardType = ParseCardType(GetStringValue(row, columnMap, Col_CardType));
                cardData.rarity = (CardRarity)GetIntValue(row, columnMap, Col_Rarity, 0);
                cardData.cost = GetIntValue(row, columnMap, Col_Cost, 0);
                cardData.waitTurns = GetIntValue(row, columnMap, Col_Wait, 0);
                cardData.durability = GetIntValue(row, columnMap, Col_Durability, 0);
                cardData.preEffect = GetStringValue(row, columnMap, Col_PreEffect);
                cardData.instantEffect = GetStringValue(row, columnMap, Col_InstantEffect);
                cardData.settleEffect = GetStringValue(row, columnMap, Col_SettleEffect);
                cardData.destroyEffect = GetStringValue(row, columnMap, Col_DestroyEffect);

                // 卡面插图：尝试从 Resources 加载（可选）
                string artPath = GetStringValue(row, columnMap, Col_ArtPath);
                if (!string.IsNullOrEmpty(artPath))
                {
                    cardData.cardArt = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
                }

                // 保存资产
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

                Debug.Log($"[导表] {(isNew ? "新增" : "更新")} 卡牌: [{cardId}] {cardName}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[导表] ✅ 导表完成！新增 {created} 张，更新 {updated} 张，跳过 {skipped} 张");
        }

        // ==================== 辅助方法 ====================

        /// <summary>
        /// 从行中读取字符串值
        /// </summary>
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

        /// <summary>
        /// 从行中读取整数值
        /// </summary>
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

        /// <summary>
        /// 将卡牌类型字符串转为枚举
        /// </summary>
        private static CardType ParseCardType(string typeString)
        {
            if (string.IsNullOrEmpty(typeString)) return CardType.Tenant;

            if (CardTypeMap.TryGetValue(typeString, out var cardType))
                return cardType;

            Debug.LogWarning($"[导表] 未识别的卡牌类型: {typeString}，默认设为 Tenant");
            return CardType.Tenant;
        }

        /// <summary>
        /// 递归创建文件夹
        /// </summary>
        private static void CreateFolderRecursive(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string currentPath = parts[0]; // "Assets"

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
