using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;

public static class ModifyExcelScript
{
    [MenuItem("Tools/BaoZuPo/Fix Card Excel Header")]
    public static void ModifyExcel()
    {
        const string excelPath = "Assets/_Assets/Data/Excel/CardData.xlsx";

        if (!File.Exists(excelPath))
        {
            Debug.LogError($"[ModifyExcelScript] File not found: {excelPath}");
            return;
        }

        IWorkbook workbook;
        using (var stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            workbook = new XSSFWorkbook(stream);
        }

        ISheet sheet = workbook.GetSheetAt(0);

        int lastRow = sheet.LastRowNum;
        if (lastRow >= 1)
        {
            sheet.ShiftRows(1, lastRow, 2);
        }

        IRow headerRowCh = sheet.GetRow(0);
        IRow headerRowEn = sheet.CreateRow(1);
        IRow headerRowType = sheet.CreateRow(2);

        for (int col = headerRowCh.FirstCellNum; col < headerRowCh.LastCellNum; col++)
        {
            ICell cellCh = headerRowCh.GetCell(col);
            if (cellCh == null)
            {
                continue;
            }

            string chName = cellCh.ToString().Trim();
            string enName = chName;
            string typeName = "string";

            switch (chName)
            {
                case "CardID":
                    enName = "cardId";
                    typeName = "int";
                    break;
                case "CardName":
                    enName = "cardName";
                    typeName = "string";
                    break;
                case "Description":
                    enName = "description";
                    typeName = "string";
                    break;
                case "CardType":
                    enName = "cardType";
                    typeName = "enum";
                    break;
                case "Rarity":
                    enName = "rarity";
                    typeName = "int";
                    break;
                case "CardArt":
                    enName = "cardArt";
                    typeName = "string";
                    break;
                case "Cost":
                    enName = "cost";
                    typeName = "int";
                    break;
                case "BaseRent":
                    enName = "baseRent";
                    typeName = "int";
                    break;
                case "TargetKind":
                    enName = "targetKind";
                    typeName = "enum";
                    break;
                case "Wait":
                    enName = "waitTurns";
                    typeName = "int";
                    break;
                case "Durability":
                    enName = "durability";
                    typeName = "int";
                    break;
                case "PreEffect":
                    enName = "preEffect";
                    typeName = "string";
                    break;
                case "InstantEffect":
                    enName = "instantEffect";
                    typeName = "string";
                    break;
                case "SettleEffect":
                    enName = "settleEffect";
                    typeName = "string";
                    break;
                case "DestroyEffect":
                    enName = "destroyEffect";
                    typeName = "string";
                    break;
            }

            headerRowEn.CreateCell(col).SetCellValue(enName);
            headerRowType.CreateCell(col).SetCellValue(typeName);
        }

        if (!HasHeader(headerRowEn, "baseRent"))
        {
            int baseRentCol = headerRowEn.LastCellNum >= 0 ? headerRowEn.LastCellNum : 0;
            headerRowCh.CreateCell(baseRentCol).SetCellValue("BaseRent");
            headerRowEn.CreateCell(baseRentCol).SetCellValue("baseRent");
            headerRowType.CreateCell(baseRentCol).SetCellValue("int");
        }

        if (!HasHeader(headerRowEn, "targetKind"))
        {
            int targetKindCol = headerRowEn.LastCellNum >= 0 ? headerRowEn.LastCellNum : 0;
            headerRowCh.CreateCell(targetKindCol).SetCellValue("TargetKind");
            headerRowEn.CreateCell(targetKindCol).SetCellValue("targetKind");
            headerRowType.CreateCell(targetKindCol).SetCellValue("enum");
        }

        using (var stream = new FileStream(excelPath, FileMode.Create, FileAccess.Write))
        {
            workbook.Write(stream);
        }

        Debug.Log("[ModifyExcelScript] Inserted English headers and type row.");
    }

    private static bool HasHeader(IRow row, string headerName)
    {
        if (row == null)
        {
            return false;
        }

        for (int col = row.FirstCellNum; col < row.LastCellNum; col++)
        {
            ICell cell = row.GetCell(col);
            if (cell != null && cell.ToString().Trim() == headerName)
            {
                return true;
            }
        }

        return false;
    }
}
