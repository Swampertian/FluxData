using DocsData.ExcelConverter.Abstractions;

namespace DocsData.ExcelConverter.Ingestion;

public sealed class ExcelFileValidator : IExcelFileValidator
{
    private static readonly string[] AllowedExtensions = [".xlsx", ".xlsm", ".xls"];

    public bool IsValid(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
