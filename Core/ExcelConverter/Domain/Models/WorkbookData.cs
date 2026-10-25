namespace DocsData.ExcelConverter.Models;

public sealed record WorkbookData(string FileName, IReadOnlyList<SheetData> Sheets);
