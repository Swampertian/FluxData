using Microsoft.AspNetCore.Components.Forms;

namespace DocsData.ExcelConverter.Abstractions;

public interface IFileIngestor
{
    Task<byte[]> ReadAsync(IBrowserFile file, CancellationToken cancellationToken = default);
}
