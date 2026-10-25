using System.Globalization;
using System.IO.Compression;
using Parquet;
using Parquet.Schema;
using DocsData.ExcelConverter.Abstractions;
using DocsData.ExcelConverter.Models;

namespace DocsData.ExcelConverter.Export;

// One .parquet file per sheet, bundled in a zip: Parquet is single-schema, so a workbook with
// sheets of different shapes can't be flattened into one file.
// this gonna be changed
public sealed class ParquetWorkbookExporter : IWorkbookExporter
{
    public async Task<byte[]> ExportAsync(WorkbookData workbook, CancellationToken cancellationToken = default)
    {
        using var zipStream = new MemoryStream();
        using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sheet in workbook.Sheets)
            {
                var entryName = $"{UniqueEntryName(sheet.Name, usedNames)}.parquet";
                var entry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
                await using var entryStream = entry.Open();
                await WriteSheetAsync(sheet, entryStream, cancellationToken);
            }
        }

        return zipStream.ToArray();
    }

    private static async Task WriteSheetAsync(SheetData sheet, Stream output, CancellationToken cancellationToken)
    {
        var fields = new DataField[sheet.ColumnNames.Count];
        for (var i = 0; i < fields.Length; i++)
        {
            fields[i] = new DataField(sheet.ColumnNames[i], sheet.ColumnTypes[i], isNullable: true);
        }

        var schema = new ParquetSchema(fields);
        await using var writer = await ParquetWriter.CreateAsync(schema, output, cancellationToken: cancellationToken);
        using var rowGroup = writer.CreateRowGroup();

        for (var c = 0; c < fields.Length; c++)
        {
            await WriteColumnAsync(rowGroup, fields[c], sheet, c, cancellationToken);
        }
    }

    private static Task WriteColumnAsync(ParquetRowGroupWriter rowGroup, DataField field, SheetData sheet, int columnIndex, CancellationToken cancellationToken)
    {
        var columnType = sheet.ColumnTypes[columnIndex];

        if (columnType == typeof(double))
        {
            return rowGroup.WriteAsync(field, Column<double>(sheet, columnIndex), cancellationToken: cancellationToken);
        }

        if (columnType == typeof(DateTime))
        {
            return rowGroup.WriteAsync(field, Column<DateTime>(sheet, columnIndex), cancellationToken: cancellationToken);
        }

        if (columnType == typeof(bool))
        {
            return rowGroup.WriteAsync(field, Column<bool>(sheet, columnIndex), cancellationToken: cancellationToken);
        }

        var strings = new string?[sheet.Rows.Count];
        for (var r = 0; r < strings.Length; r++)
        {
            strings[r] = ToInvariantString(sheet.Rows[r][columnIndex]);
        }
        return rowGroup.WriteAsync(field, strings!);
    }

    private static ReadOnlyMemory<T?> Column<T>(SheetData sheet, int columnIndex) where T : struct
    {
        var values = new T?[sheet.Rows.Count];
        for (var r = 0; r < values.Length; r++)
        {
            values[r] = sheet.Rows[r][columnIndex] is T typed ? typed : null;
        }
        return values;
    }

    private static string? ToInvariantString(object? value) => value switch
    {
        null => null,
        string s => s,
        DateTime dateTime => dateTime.ToString("s", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    private static string UniqueEntryName(string sheetName, HashSet<string> usedNames)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(sheetName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "sheet";
        }

        var candidate = sanitized;
        var suffix = 1;
        while (!usedNames.Add(candidate))
        {
            candidate = $"{sanitized}_{suffix++}";
        }
        return candidate;
    }
}
