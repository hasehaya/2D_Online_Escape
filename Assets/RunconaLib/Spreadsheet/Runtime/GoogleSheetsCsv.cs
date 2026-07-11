using System;

namespace RunconaLib.Spreadsheet
{
    public static class GoogleSheetsCsv
    {
        public static string BuildExportUrl(string spreadsheetId, string sheetId = "0")
        {
            if (string.IsNullOrWhiteSpace(spreadsheetId))
                throw new ArgumentException("Spreadsheet ID is required.", nameof(spreadsheetId));
            return
                $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={Uri.EscapeDataString(sheetId ?? "0")}";
        }
    }
}