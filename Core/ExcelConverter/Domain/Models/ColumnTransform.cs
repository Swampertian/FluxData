namespace DocsData.ExcelConverter.Models;

// Desired final name/type for one column, index-aligned to the source SheetData.ColumnNames.
// Type must be one of the types ParquetWorkbookExporter understands (double, DateTime, bool,
// string) since those are the only ones the Export stage writes natively.
// Include=false drops the column from the output. The list stays index-aligned to the source on
// purpose: a compacted list can't say which source column each entry came from, so an excluded
// column is marked here rather than removed.
public sealed record ColumnTransform(string Name, Type Type, bool Include = true);
