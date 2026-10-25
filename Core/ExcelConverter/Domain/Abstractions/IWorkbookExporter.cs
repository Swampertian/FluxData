using DocsData.ExcelConverter.Models;

namespace DocsData.ExcelConverter.Abstractions;

public interface IWorkbookExporter
{
    Task<byte[]> ExportAsync(WorkbookData workbook, CancellationToken cancellationToken = default);
}
