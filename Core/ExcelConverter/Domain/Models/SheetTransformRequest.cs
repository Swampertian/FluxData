namespace DocsData.ExcelConverter.Models;

// Columns must be index-aligned to the target sheet's SheetData.ColumnNames/ColumnTypes.
// TargetName renames the sheet, which is what the Export stage uses as the table/file name;
// null or blank keeps SheetName.
public sealed record SheetTransformRequest(
    string SheetName,
    IReadOnlyList<ColumnTransform> Columns,
    string? TargetName = null);
