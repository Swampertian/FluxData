using System.Diagnostics;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using DocsData.ExcelConverter.Abstractions;
using DocsData.ExcelConverter.Models;

namespace DocsData.ExcelConverter.Pipeline;

public sealed class ExcelPipeline(
    IExcelFileValidator fileValidator,
    IFileIngestor fileIngestor,
    IWorkbookParser workbookParser,
    IWorkbookTransformer workbookTransformer,
    IWorkbookExporter workbookExporter,
    ILogger<ExcelPipeline> logger) : IExcelPipeline
{
    public async Task<UploadResult> UploadAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        RunStage(PipelineStage.Validation, () =>
        {
            if (!fileValidator.IsValid(file.Name))
            {
                throw new ExcelConverterException("Formato inválido. Envie um arquivo Excel (.xlsx, .xlsm ou .xls).");
            }
        });

        var content = await RunStageAsync(PipelineStage.Ingestion, () => fileIngestor.ReadAsync(file, cancellationToken));

        return new UploadResult(file.Name, content);
    }

    public Task<WorkbookData> ProcessAsync(byte[] content, string fileName, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => RunStage(PipelineStage.Parsing, () => workbookParser.Parse(content, fileName)), cancellationToken);
    }

    public Task<WorkbookData> TransformAsync(WorkbookData workbook, WorkbookTransformRequest request, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => RunStage(PipelineStage.Transform, () => workbookTransformer.Transform(workbook, request)), cancellationToken);
    }

    public Task<byte[]> ExportAsync(WorkbookData workbook, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => RunStageAsync(PipelineStage.Export, () => workbookExporter.ExportAsync(workbook, cancellationToken)), cancellationToken);
    }

    private void RunStage(PipelineStage stage, Action action) => RunStage<object?>(stage, () =>
    {
        action();
        return null;
    });

    private T RunStage<T>(PipelineStage stage, Func<T> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = action();
            logger.LogInformation("[Pipeline] {Stage} ok em {ElapsedMs}ms", stage, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (ExcelConverterException ex)
        {
            logger.LogError(ex, "[Pipeline] {Stage} falhou", stage);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Pipeline] {Stage} falhou", stage);
            throw new ExcelConverterException($"Falha na etapa de {stage}.", ex);
        }
    }

    private async Task<T> RunStageAsync<T>(PipelineStage stage, Func<Task<T>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await action();
            logger.LogInformation("[Pipeline] {Stage} ok em {ElapsedMs}ms", stage, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (ExcelConverterException ex)
        {
            logger.LogError(ex, "[Pipeline] {Stage} falhou", stage);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Pipeline] {Stage} falhou", stage);
            throw new ExcelConverterException($"Falha na etapa de {stage}.", ex);
        }
    }
}
