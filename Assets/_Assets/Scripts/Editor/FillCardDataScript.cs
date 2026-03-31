using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;

namespace BaoZuPo.Editor
{
    /// <summary>
    /// Rewrites the card Excel source of truth from the editor-side catalog.
    /// </summary>
    public static class FillCardDataScript
    {
        private const string ExcelPath = "Assets/_Assets/Data/Excel/CardData.xlsx";
        private const int DataStartRowIndex = 3;
        private const int SheetIndex = 0;
        private const int ColumnCount = 16;

        [MenuItem("Tools/BaoZuPo/Fill 50 Cards")]
        public static void FillCards()
        {
            if (!File.Exists(ExcelPath))
            {
                Debug.LogError($"[FillCardDataScript] File not found: {ExcelPath}");
                return;
            }

            IWorkbook workbook;
            using (var stream = new FileStream(ExcelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                workbook = new XSSFWorkbook(stream);
            }

            ISheet sheet = workbook.GetSheetAt(SheetIndex);
            if (sheet == null)
            {
                Debug.LogError($"[FillCardDataScript] Sheet {SheetIndex} not found.");
                return;
            }

            for (int rowIndex = sheet.LastRowNum; rowIndex >= DataStartRowIndex; rowIndex--)
            {
                IRow row = sheet.GetRow(rowIndex);
                if (row != null)
                {
                    sheet.RemoveRow(row);
                }
            }

            int rowIdx = DataStartRowIndex;
            foreach (var card in CardSheetCatalog.Rows)
            {
                WriteRow(sheet, rowIdx++, card);
            }

            using (var stream = new FileStream(ExcelPath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(stream);
            }

            Debug.Log($"[FillCardDataScript] Wrote {rowIdx - DataStartRowIndex} cards to Excel. Run Tools/BaoZuPo/Import Card Data on the main thread to generate assets.");
        }

        private static void WriteRow(ISheet sheet, int rowIdx, CardSheetCatalog.CardRow card)
        {
            IRow row = sheet.CreateRow(rowIdx);
            row.CreateCell(0).SetCellValue(card.CardId);
            row.CreateCell(1).SetCellValue(card.CardName);
            row.CreateCell(2).SetCellValue(card.Description);
            row.CreateCell(3).SetCellValue(card.CardType);
            row.CreateCell(4).SetCellValue(card.Rarity);
            row.CreateCell(5).SetCellValue(card.CardArt);
            row.CreateCell(6).SetCellValue(card.Cost);
            row.CreateCell(7).SetCellValue(card.BaseRent);
            row.CreateCell(8).SetCellValue(card.TargetKind);
            row.CreateCell(9).SetCellValue(card.WaitTurns);
            row.CreateCell(10).SetCellValue(card.Durability);
            row.CreateCell(11).SetCellValue(card.PreEffect);
            row.CreateCell(12).SetCellValue(card.InstantEffect);
            row.CreateCell(13).SetCellValue(card.SettleEffect);
            row.CreateCell(14).SetCellValue(card.DestroyEffect);
            row.CreateCell(15).SetCellValue(card.Tag);

            for (int col = row.LastCellNum; col < ColumnCount; col++)
            {
                row.CreateCell(col);
            }
        }
    }
}
