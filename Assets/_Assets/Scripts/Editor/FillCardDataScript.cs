using System.IO;
using UnityEditor;
using UnityEngine;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

/// <summary>
/// 一次性脚本：向 CardData.xlsx 填充 20 张卡牌数据
/// 使用后可删除此文件
/// </summary>
public static class FillCardDataScript
{
    [MenuItem("Tools/包租婆/填充20张卡牌数据（一次性）")]
    public static void FillCards()
    {
        string excelPath = "Assets/_Assets/Data/Excel/CardData.xlsx";

        if (!File.Exists(excelPath))
        {
            Debug.LogError($"[脚本] 找不到: {excelPath}");
            return;
        }

        IWorkbook workbook;
        using (var stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            workbook = new XSSFWorkbook(stream);
        }

        ISheet sheet = workbook.GetSheetAt(0);

        // 先清除旧数据行（从第4行，即索引3开始）
        for (int i = sheet.LastRowNum; i >= 3; i--)
        {
            IRow row = sheet.GetRow(i);
            if (row != null) sheet.RemoveRow(row);
        }

        // 表头列顺序（与英文表头一致）：
        // 0:cardId, 1:cardName, 2:description, 3:cardType, 4:rarity,
        // 5:cardArt, 6:cost, 7:waitTurns, 8:durability, 
        // 9:preEffect, 10:instantEffect, 11:settleEffect, 12:destroyEffect

        int rowIdx = 3; // 数据从第4行开始（索引3）

        // ==================== Tenants ×10 ====================
        WriteRow(sheet, rowIdx++, 1001, "Worker", "Simple worker, pays on time.",
            "Card_Tenant", 0, "", 50, 0, 5, "", "AddMoney;60", "AddMoney;30", "ReduceMoney;30");

        WriteRow(sheet, rowIdx++, 1002, "White Collar", "Stable income, clean room.",
            "Card_Tenant", 1, "", 120, 0, 8, "", "AddMoney;120", "AddMoney;60", "ReduceMoney;60");

        WriteRow(sheet, rowIdx++, 1003, "Programmer", "High pay, often overtimes.",
            "Card_Tenant", 2, "", 200, 0, 4, "", "AddMoney;200", "AddMoney;100", "ReduceMoney;100");

        WriteRow(sheet, rowIdx++, 1004, "Senior Teacher", "Quiet old tenant, never leaves.",
            "Card_Tenant", 1, "", 80, 0, 0, "", "AddMoney;50", "AddMoney;25", "ReduceMoney;25");

        WriteRow(sheet, rowIdx++, 1005, "Student", "Cheap but short lease.",
            "Card_Tenant", 0, "", 30, 0, 4, "", "AddMoney;40", "AddMoney;20", "ReduceMoney;20");

        WriteRow(sheet, rowIdx++, 1006, "Streamer", "Lavish pay but moves often.",
            "Card_Tenant", 2, "", 150, 0, 3, "", "AddMoney;240", "AddMoney;120", "ReduceMoney;120");

        WriteRow(sheet, rowIdx++, 1007, "Doctor", "High deposit, long lease.",
            "Card_Tenant", 2, "", 250, 0, 10, "", "AddMoney;160", "AddMoney;80", "ReduceMoney;80");

        WriteRow(sheet, rowIdx++, 1008, "Chef", "Sturdy and reliable.",
            "Card_Tenant", 1, "", 100, 0, 6, "", "AddMoney;100", "AddMoney;50", "ReduceMoney;50");

        WriteRow(sheet, rowIdx++, 1009, "Painter", "Needs time to settle in.",
            "Card_Tenant", 1, "", 60, 2, 6, "", "AddMoney;80", "AddMoney;40", "ReduceMoney;40");

        WriteRow(sheet, rowIdx++, 1010, "Courier", "Comes fast, goes fast.",
            "Card_Tenant", 0, "", 40, 0, 3, "", "AddMoney;70", "AddMoney;35", "ReduceMoney;35");

        // ==================== Equipment ×5 ====================
        WriteRow(sheet, rowIdx++, 2001, "Air Conditioner", "Improves comfort.",
            "Card_Equipt", 1, "", 80, 0, 5, "", "", "AddMoney;10", "");

        WriteRow(sheet, rowIdx++, 2002, "Bunk Bed", "One more tenant.",
            "Card_Equipt", 2, "", 100, 0, 0, "", "ExpandSlot;1", "", "");

        WriteRow(sheet, rowIdx++, 2003, "Water Heater", "Basic facility, durable.",
            "Card_Equipt", 0, "", 60, 0, 8, "", "", "AddMoney;5", "");

        WriteRow(sheet, rowIdx++, 2004, "WiFi Router", "Life necessity.",
            "Card_Equipt", 1, "", 90, 0, 6, "", "", "AddMoney;15", "");

        WriteRow(sheet, rowIdx++, 2005, "Washer", "Laundry convenience.",
            "Card_Equipt", 1, "", 70, 0, 7, "", "", "AddMoney;8", "");

        // ==================== Event ×5 ====================
        WriteRow(sheet, rowIdx++, 3001, "Rent Bonus", "Extra rent income.",
            "Card_Event", 0, "", 0, 0, 0, "", "AddMoney;200", "", "");

        WriteRow(sheet, rowIdx++, 3002, "Agent Refer", "Pay for 2 more cards.",
            "Card_Event", 1, "", 50, 0, 0, "", "DrawCard;2", "", "");

        WriteRow(sheet, rowIdx++, 3003, "Renewal", "Persuade all to stay.",
            "Card_Event", 1, "", 80, 0, 0, "", "AddTenantDurability;3", "", "");

        WriteRow(sheet, rowIdx++, 3004, "Maintenance", "Repair all equipment.",
            "Card_Event", 1, "", 60, 0, 0, "", "AddEquipmentDurability;3", "", "");

        WriteRow(sheet, rowIdx++, 3005, "Urgent Loan", "Bank emergency cash.",
            "Card_Event", 2, "", 0, 0, 0, "", "AddMoney;500", "", "");

        // 保存
        using (var stream = new FileStream(excelPath, FileMode.Create, FileAccess.Write))
        {
            workbook.Write(stream);
        }

        Debug.Log($"[脚本] 成功填充 {rowIdx - 3} 张卡牌数据到 Excel！请执行 Tools → 包租婆 → 导入卡牌表");
    }

    private static void WriteRow(ISheet sheet, int rowIdx,
        int cardId, string cardName, string description,
        string cardType, int rarity, string cardArt,
        int cost, int waitTurns, int durability,
        string preEffect, string instantEffect, string settleEffect, string destroyEffect)
    {
        IRow row = sheet.CreateRow(rowIdx);
        row.CreateCell(0).SetCellValue(cardId);
        row.CreateCell(1).SetCellValue(cardName);
        row.CreateCell(2).SetCellValue(description);
        row.CreateCell(3).SetCellValue(cardType);
        row.CreateCell(4).SetCellValue(rarity);
        row.CreateCell(5).SetCellValue(cardArt);
        row.CreateCell(6).SetCellValue(cost);
        row.CreateCell(7).SetCellValue(waitTurns);
        row.CreateCell(8).SetCellValue(durability);
        row.CreateCell(9).SetCellValue(preEffect);
        row.CreateCell(10).SetCellValue(instantEffect);
        row.CreateCell(11).SetCellValue(settleEffect);
        row.CreateCell(12).SetCellValue(destroyEffect);
    }
}
