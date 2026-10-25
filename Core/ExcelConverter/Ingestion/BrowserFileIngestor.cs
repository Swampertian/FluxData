using Microsoft.AspNetCore.Components.Forms;
using DocsData.ExcelConverter.Abstractions;

namespace DocsData.ExcelConverter.Ingestion;

public sealed class BrowserFileIngestor : IFileIngestor
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;

    public async Task<byte[]> ReadAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        if (file.Size > MaxFileSizeBytes)
        {
            throw new ExcelConverterException("Arquivo excede o tamanho máximo permitido (25MB).");
        }

        await using var stream = file.OpenReadStream(MaxFileSizeBytes, cancellationToken);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        return memory.ToArray();
    }
}
