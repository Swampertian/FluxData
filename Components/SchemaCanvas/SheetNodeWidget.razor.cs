using Microsoft.AspNetCore.Components;
using ColumnState = DocsData.Components.SheetCard.SheetCard.ColumnState;

namespace DocsData.Components.SchemaCanvas;

public partial class SheetNodeWidget
{
    [Parameter] public SheetNode Node { get; set; } = null!;

    private void OnIncludedChanged(bool included)
    {
        Node.Sheet.Included = included;
        Node.OnChanged?.Invoke();
    }

    private void OnTargetNameChanged(string targetName)
    {
        Node.Sheet.TargetName = targetName;
        Node.OnChanged?.Invoke();
    }

    private void OnColumnChanged(ColumnState column)
    {
        Node.OnChanged?.Invoke();
    }
}
