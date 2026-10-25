namespace DocsData.ExcelConverter.Models;

// The first row of the sheet is treated as the header (column names); ColumnTypes is inferred
// from the CLR type of the cell values returned by the reader (double/DateTime/bool/string),
// falling back to string for a column whose data rows mix types.
public sealed record SheetData(
    string Name,
    IReadOnlyList<string> ColumnNames,
    IReadOnlyList<Type> ColumnTypes,
    IReadOnlyList<object?[]> Rows)
{
    public int RowCount => Rows.Count;
}
