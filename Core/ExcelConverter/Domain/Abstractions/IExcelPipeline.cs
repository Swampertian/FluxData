using Microsoft.AspNetCore.Components.Forms;
using DocsData.ExcelConverter.Models;

namespace DocsData.ExcelConverter.Abstractions;

public interface IExcelPipeline
{
    Task<UploadResult> UploadAsync(IBrowserFile file, CancellationToken cancellationToken = default);

    Task<WorkbookData> ProcessAsync(byte[] content, string fileName, CancellationToken cancellationToken = default);

    Task<WorkbookData> TransformAsync(WorkbookData workbook, WorkbookTransformRequest request, CancellationToken cancellationToken = default);

    Task<byte[]> ExportAsync(WorkbookData workbook, CancellationToken cancellationToken = default);
}
