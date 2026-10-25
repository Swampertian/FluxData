using DocsData.Components.SheetCard;

namespace DocsData.Pages;

// Mirrors one sheet of the current workbook as UI-editable state: include/exclude, rename, and
// per-column include/rename/retype, all pending until BuildTransformRequest sends them to the
// Transform stage.
public sealed class SheetEditState
{
    public required string SheetName { get; init; }
    public required int RowCount { get; init; }
    public required int ColumnCount { get; init; }
    public required List<SheetCard.ColumnState> Columns { get; init; }
    public bool Included { get; set; } = true;
    public required string TargetName { get; set; }
}
