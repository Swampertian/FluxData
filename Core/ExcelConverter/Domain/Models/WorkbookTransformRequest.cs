namespace DocsData.ExcelConverter.Models;

// The selection of what gets exported: a sheet with no matching entry here is dropped from the
// workbook by the Transform stage.
public sealed record WorkbookTransformRequest(IReadOnlyList<SheetTransformRequest> Sheets);
