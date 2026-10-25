using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using DocsData.Components.SchemaCanvas;
using DocsData.Components.SheetCard;
using DocsData.ExcelConverter;
using DocsData.ExcelConverter.Abstractions;
using DocsData.ExcelConverter.Models;

namespace DocsData.Pages;

public partial class Home
{
    private const int NodesPerRow = 3;
    private const double NodeSpacingX = 460;
    private const double NodeSpacingY = 340;
    private const double NodeStartX = 80;
    private const double NodeStartY = 220;

    [Inject] private IExcelPipeline Pipeline { get; set; } = null!;

    public BlazorDiagram Diagram { get; } = new();

    private UploadResult? upload;
    private WorkbookData? workbookData;
    private List<SheetEditState> sheetEdits = [];
    private string? errorMessage;
    private bool isProcessing;
    private bool isExporting;
    private bool isTransforming;
    private bool hasAppliedTransform;

    private int currentStep => workbookData is null
        ? (upload is null ? 0 : 1)
        : (hasAppliedTransform ? 3 : 2);

    private bool hasPendingSelection => sheetEdits.Any(sheet =>
        !sheet.Included
        || !string.Equals(sheet.TargetName, sheet.SheetName, StringComparison.Ordinal)
        || sheet.Columns.Any(column =>
            !column.Include
            || !string.Equals(column.Name, column.OriginalName, StringComparison.Ordinal)
            || !string.Equals(column.TypeName, column.OriginalTypeName, StringComparison.Ordinal)));

    protected override void OnInitialized()
    {
        Diagram.RegisterComponent<SheetNode, SheetNodeWidget>();
    }

    private async Task OnFileSelected(IBrowserFile file)
    {
        errorMessage = null;
        upload = null;
        workbookData = null;
        sheetEdits = [];
        hasAppliedTransform = false;
        Diagram.Nodes.Clear();

        try
        {
            upload = await Pipeline.UploadAsync(file);
        }
        catch (ExcelConverterException ex)
        {
            errorMessage = ex.Message;
        }
    }

    private async Task ProcessAsync()
    {
        if (upload is null) return;

        errorMessage = null;
        isProcessing = true;
        hasAppliedTransform = false;

        try
        {
            workbookData = await Pipeline.ProcessAsync(upload.Content, upload.FileName);
            sheetEdits = SheetEditMapper.BuildSheetEdits(workbookData);
            RebuildDiagramNodes();
        }
        catch (ExcelConverterException ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isProcessing = false;
        }
    }

    private async Task ApplyTransformAsync()
    {
        if (workbookData is null) return;

        errorMessage = null;

        var validationError = SheetSelectionValidator.Validate(sheetEdits);
        if (validationError is not null)
        {
            errorMessage = validationError;
            return;
        }

        isTransforming = true;

        try
        {
            var request = SheetEditMapper.BuildTransformRequest(sheetEdits);
            workbookData = await Pipeline.TransformAsync(workbookData, request);
            sheetEdits = SheetEditMapper.BuildSheetEdits(workbookData);
            hasAppliedTransform = true;
            RebuildDiagramNodes();
        }
        catch (ExcelConverterException ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isTransforming = false;
        }
    }

    private async Task ExportAsync()
    {
        if (workbookData is null) return;

        if (hasPendingSelection)
        {
            errorMessage = "Aplique as alteracoes antes de exportar.";
            return;
        }

        errorMessage = null;
        isExporting = true;

        try
        {
            var zipBytes = await Pipeline.ExportAsync(workbookData);
            var fileName = $"{Path.GetFileNameWithoutExtension(workbookData.FileName)}.zip";
            await JS.InvokeVoidAsync("downloadFileFromBytes", fileName, zipBytes);
        }
        catch (ExcelConverterException ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isExporting = false;
        }
    }

    private void Reset()
    {
        upload = null;
        workbookData = null;
        sheetEdits = [];
        errorMessage = null;
        hasAppliedTransform = false;
        Diagram.Nodes.Clear();
    }
    private void GoToStep(int targetStep)
    {
        if (targetStep >= currentStep) return;

        errorMessage = null;

        switch (targetStep)
        {
            case 0:
                Reset();
                break;
            case 1:
                workbookData = null;
                sheetEdits = [];
                hasAppliedTransform = false;
                Diagram.Nodes.Clear();
                break;
            case 2:
                hasAppliedTransform = false;
                break;
        }
    }

    private void RebuildDiagramNodes()
    {
        Diagram.Nodes.Clear();

        for (var i = 0; i < sheetEdits.Count; i++)
        {
            var column = i % NodesPerRow;
            var row = i / NodesPerRow;
            var position = new Point(NodeStartX + column * NodeSpacingX, NodeStartY + row * NodeSpacingY);
            Diagram.Nodes.Add(new SheetNode(sheetEdits[i], position, StateHasChanged));
        }
    }
}
