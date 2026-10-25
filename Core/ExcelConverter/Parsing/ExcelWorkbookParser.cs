using Sylvan.Data.Excel;
using DocsData.ExcelConverter.Abstractions;
using DocsData.ExcelConverter.Models;

namespace DocsData.ExcelConverter.Parsing;

// Single reader instance, single pass over the source bytes: each cell is read exactly once,
// straight into the final row array as its original CLR value (no early stringification) so
// column types survive for the export stage. No RawSheet/second-pass materialization either.
public sealed class ExcelWorkbookParser : IWorkbookParser
{
    public WorkbookData Parse(byte[] content, string fileName)
    {
        var workbookType = ExcelDataReader.GetWorkbookType(fileName);

        using var stream = new MemoryStream(content);
        using var reader = ExcelDataReader.Create(stream, workbookType);

        var sheets = new List<SheetData>();
        do
        {
            sheets.Add(ParseSheet(reader));
        } while (reader.NextResult());

        return new WorkbookData(fileName, sheets);
    }

    private static SheetData ParseSheet(ExcelDataReader reader)
    {
        var sheetName = reader.WorksheetName ?? string.Empty;
        var columnCount = reader.FieldCount;
        var columnNames = new string[columnCount];
        for (var i = 0; i < columnCount; i++)
        {
            var header = reader.GetName(i);
            columnNames[i] = string.IsNullOrWhiteSpace(header) ? $"column_{i}" : header;
        }

        var columnTypes = new Type?[columnCount];
        var rows = new List<object?[]>();

        while (reader.Read())
        {
            var fieldCount = reader.RowFieldCount;
            if (fieldCount > columnCount)
            {
                columnCount = fieldCount;
                Array.Resize(ref columnNames, columnCount);
                Array.Resize(ref columnTypes, columnCount);
            }

            var row = new object?[fieldCount];
            for (var i = 0; i < fieldCount; i++)
            {
                var value = ReadCell(reader, i);
                row[i] = value;

                if (value is not null)
                {
                    var valueType = value.GetType();
                    columnTypes[i] = columnTypes[i] switch
                    {
                        null => valueType,
                        var t when t == valueType => t,
                        _ => typeof(string)
                    };
                }
            }
            rows.Add(row);
        }

        for (var i = 0; i < columnCount; i++)
        {
            if (string.IsNullOrEmpty(columnNames[i]))
            {
                columnNames[i] = $"column_{i}";
            }
            columnTypes[i] ??= typeof(string);
        }

        for (var r = 0; r < rows.Count; r++)
        {
            if (rows[r].Length < columnCount)
            {
                var padded = new object?[columnCount];
                Array.Copy(rows[r], padded, rows[r].Length);
                rows[r] = padded;
            }
        }

        return new SheetData(sheetName, columnNames, columnTypes!, rows);
    }

    // Typed accessors instead of GetValue: the default schema stringifies every cell, and
    // ExcelSchema.Dynamic splits numbers into Int32/Double and yields TimeSpan, none of which
    // fit the double/DateTime/bool/string set the export stage writes. Time-only cells stay text.
    private static object? ReadCell(ExcelDataReader reader, int ordinal)
    {
        return reader.GetExcelDataType(ordinal) switch
        {
            ExcelDataType.Null => null,
            ExcelDataType.Error => $"#{reader.GetFormulaError(ordinal)}",
            ExcelDataType.Boolean => reader.GetBoolean(ordinal),
            ExcelDataType.DateTime => reader.GetDateTime(ordinal),
            ExcelDataType.Numeric => reader.GetFormat(ordinal)?.Kind switch
            {
                FormatKind.Date => reader.GetDateTime(ordinal),
                FormatKind.Time => reader.GetString(ordinal),
                _ => reader.GetDouble(ordinal)
            },
            _ => reader.GetString(ordinal)
        };
    }
}
