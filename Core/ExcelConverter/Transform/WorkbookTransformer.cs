using DocsData.ExcelConverter.Abstractions;
using DocsData.ExcelConverter.Models;

namespace DocsData.ExcelConverter.Transform;

// Applies the user's selection (which sheets, which columns) plus renames/type overrides after
// Parsing and before Export. Parsing never sees these overrides — it only ever returns Sylvan's
// raw CLR values — so this is the one place a column's declared Type is made to match every row's
// actual runtime value, which is what ParquetWorkbookExporter's unboxing cast relies on.
// Filtering happens here rather than in Parsing: the parser reads the whole workbook in one
// forward-only pass, and skipping sheets there would save CPU at the cost of re-parsing whenever
// the selection changes.
public sealed class WorkbookTransformer : IWorkbookTransformer
{
    public WorkbookData Transform(WorkbookData workbook, WorkbookTransformRequest request)
    {
        var requestsByName = request.Sheets.ToDictionary(s => s.SheetName, StringComparer.Ordinal);

        var sheets = workbook.Sheets
            .Where(sheet => requestsByName.ContainsKey(sheet.Name))
            .Select(sheet => TransformSheet(sheet, requestsByName[sheet.Name]))
            .ToList();

        if (sheets.Count == 0)
        {
            throw new ExcelConverterException("Selecione ao menos uma planilha para continuar.");
        }

        return workbook with { Sheets = sheets };
    }

    private static SheetData TransformSheet(SheetData sheet, SheetTransformRequest request)
    {
        var sourceIndexes = SelectedColumns(sheet, request);
        if (sourceIndexes.Count == 0)
        {
            throw new ExcelConverterException($"A planilha '{sheet.Name}' precisa de ao menos uma coluna selecionada.");
        }

        var columnCount = sourceIndexes.Count;
        var columnNames = new string[columnCount];
        var columnTypes = new Type[columnCount];

        for (var i = 0; i < columnCount; i++)
        {
            var source = sourceIndexes[i];
            var column = source < request.Columns.Count ? request.Columns[source] : null;
            columnNames[i] = string.IsNullOrWhiteSpace(column?.Name) ? sheet.ColumnNames[source] : column.Name;
            columnTypes[i] = column?.Type ?? sheet.ColumnTypes[source];
        }

        var rows = new List<object?[]>(sheet.Rows.Count);
        foreach (var sourceRow in sheet.Rows)
        {
            var row = new object?[columnCount];
            for (var i = 0; i < columnCount; i++)
            {
                var source = sourceIndexes[i];
                row[i] = columnTypes[i] == sheet.ColumnTypes[source]
                    ? sourceRow[source]
                    : CellTypeConverter.Convert(sourceRow[source], columnTypes[i]);
            }
            rows.Add(row);
        }

        var name = string.IsNullOrWhiteSpace(request.TargetName) ? sheet.Name : request.TargetName.Trim();

        return sheet with { Name = name, ColumnNames = columnNames, ColumnTypes = columnTypes, Rows = rows };
    }

    // Source column indexes that survive into the output, in source order. A source column past
    // the end of the request list has no entry and is kept.
    private static List<int> SelectedColumns(SheetData sheet, SheetTransformRequest request)
    {
        var selected = new List<int>(sheet.ColumnNames.Count);
        for (var i = 0; i < sheet.ColumnNames.Count; i++)
        {
            if (i >= request.Columns.Count || request.Columns[i].Include)
            {
                selected.Add(i);
            }
        }
        return selected;
    }
}
