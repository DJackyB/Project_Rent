using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

public static class ModifyExcelScript
{
    [MenuItem("Tools/包租婆/修复卡牌表表头（一次性使用）")]
    public static void ModifyExcel()
    {
        string excelPath = "Assets/_Assets/Data/Excel/CardData.xlsx";
        
        if (!File.Exists(excelPath))
        {
            Debug.LogError($"[脚本] 找不到这表: {excelPath}");
            return;
        }

        IWorkbook workbook;
        using (var stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            workbook = new XSSFWorkbook(stream);
        }

        ISheet sheet = workbook.GetSheetAt(0);

        // 插入两行：在第一行（索引0，中文）之后，我们需要插入两行。
        // 所以我们把第 1行（索引1，原来是数据）及之后的行往下推 2 行。
        int lastRow = sheet.LastRowNum;
        if (lastRow >= 1) {
            sheet.ShiftRows(1, lastRow, 2);
        }

        IRow headerRowCh = sheet.GetRow(0); // 中文表头
        
        // 创建第二行：英文字段名
        IRow headerRowEn = sheet.CreateRow(1);
        
        // 创建第三行：字段类型
        IRow headerRowType = sheet.CreateRow(2);

        // 建立一个映射，根据中文名称写入英文名和类型
        for (int col = headerRowCh.FirstCellNum; col < headerRowCh.LastCellNum; col++)
        {
            ICell cellCh = headerRowCh.GetCell(col);
            if (cellCh == null) continue;
            
            string chName = cellCh.ToString().Trim();
            string enName = "";
            string typeName = "string";

            switch (chName)
            {
                case "卡牌ID": enName = "cardId"; typeName = "int"; break;
                case "卡牌名称": enName = "cardName"; typeName = "string"; break;
                case "卡牌说明": enName = "description"; typeName = "string"; break;
                case "卡牌类型": enName = "cardType"; typeName = "enum"; break;
                case "卡牌稀有度": enName = "rarity"; typeName = "int"; break;
                case "卡面插图": enName = "cardArt"; typeName = "string"; break;
                case "花费": enName = "cost"; typeName = "int"; break;
                case "等待": enName = "waitTurns"; typeName = "int"; break;
                case "耐久": enName = "durability"; typeName = "int"; break;
                case "前置效果": enName = "preEffect"; typeName = "string"; break;
                case "即时效果": enName = "instantEffect"; typeName = "string"; break;
                case "结算效果": enName = "settleEffect"; typeName = "string"; break;
                case "销毁效果": enName = "destroyEffect"; typeName = "string"; break;
                default: enName = chName; break; // 如果有未知的，沿用旧的
            }

            headerRowEn.CreateCell(col).SetCellValue(enName);
            headerRowType.CreateCell(col).SetCellValue(typeName);
        }

        using (var stream = new FileStream(excelPath, FileMode.Create, FileAccess.Write))
        {
            workbook.Write(stream);
        }
        
        Debug.Log("[脚本] 成功在第二行和第三行插入了英文字段和类型！请打开 Excel 查看。");
    }
}
