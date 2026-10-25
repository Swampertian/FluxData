using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using DocsData.Pages;

namespace DocsData.Components.SchemaCanvas;

// Wraps one SheetEditState so it can be dragged around the Blazor.Diagrams canvas, rendered via
// SheetNodeWidget so it can host the real SheetCard component. OnChanged lets the page (which
// owns hasPendingSelection/currentStep, rendered outside the diagram's component subtree) know
// when an edit inside this node should re-render the toolbar.
public sealed class SheetNode : NodeModel
{
    public SheetNode(SheetEditState sheet, Point? position = null, Action? onChanged = null) : base(position)
    {
        Sheet = sheet;
        OnChanged = onChanged;
    }

    public SheetEditState Sheet { get; }
    public Action? OnChanged { get; }
}
