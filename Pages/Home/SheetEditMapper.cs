using DocsData.Components.SheetCard;
using DocsData.ExcelConverter.Models;

namespace DocsData.Pages;

// Maps between WorkbookData (domain) and SheetEditState (UI-editable state), and back into a
// WorkbookTransformRequest for the Transform stage.
public static class SheetEditMapper
{
    private static readonly IReadOnlyDictionary<string, Type> ColumnTypesByName = new Dictionary<string, Type>
    {
        ["string"] = typeof(string),
        ["double"] = typeof(double),
        ["datetime"] = typeof(DateTime),
        ["bool"] = typeof(bool)
    };

    public static List<SheetEditState> BuildSheetEdits(WorkbookData workbook) =>
        workbook.Sheets.Select(sheet => new SheetEditState
        {
            SheetName = sheet.Name,
            TargetName = sheet.Name,
            RowCount = sheet.RowCount,
            ColumnCount = sheet.ColumnNames.Count,
            Columns = sheet.ColumnNames.Select((name, i) =>
            {
                var typeName = ColumnTypesByName.FirstOrDefault(kv => kv.Value == sheet.ColumnTypes[i]).Key ?? "string";
                return new SheetCard.ColumnState
                {
                    OriginalName = name,
                    OriginalTypeName = typeName,
                    Name = name,
                    TypeName = typeName
                };
            }).ToList()
        }).ToList();

    public static WorkbookTransformRequest BuildTransformRequest(IEnumerable<SheetEditState> sheetEdits)
    {
        // Only the selected sheets are sent: a sheet absent from the request is dropped by the
        // Transform stage. Columns stay index-aligned to the source, so an unselected column is
        // sent with Include=false instead of being left out of the list.
        var sheets = sheetEdits
            .Where(sheet => sheet.Included)
            .Select(sheet => new SheetTransformRequest(
                sheet.SheetName,
                sheet.Columns
                    .Select(c => new ColumnTransform(
                        c.Name,
                        ColumnTypesByName.GetValueOrDefault(c.TypeName, typeof(string)),
                        c.Include))
                    .ToList(),
                sheet.TargetName))
            .ToList();

        return new WorkbookTransformRequest(sheets);
    }
}
