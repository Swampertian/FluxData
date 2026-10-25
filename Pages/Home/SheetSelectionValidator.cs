namespace DocsData.Pages;

// Validates the current sheet/column selection before it's sent to the Transform stage. Pure
// function over SheetEditState, no Blazor dependencies.
public static class SheetSelectionValidator
{
    public static string? Validate(IEnumerable<SheetEditState> sheetEdits)
    {
        var selected = sheetEdits.Where(sheet => sheet.Included).ToList();
        if (selected.Count == 0)
        {
            return "Selecione ao menos uma planilha para continuar.";
        }

        var withoutColumns = selected.FirstOrDefault(sheet => !sheet.Columns.Any(c => c.Include));
        if (withoutColumns is not null)
        {
            return $"A planilha '{withoutColumns.SheetName}' precisa de ao menos uma coluna selecionada.";
        }

        var blankName = selected.FirstOrDefault(sheet => string.IsNullOrWhiteSpace(sheet.TargetName));
        if (blankName is not null)
        {
            return $"Informe um nome de tabela para a planilha '{blankName.SheetName}'.";
        }

        var duplicateSheet = FindSheetWithDuplicateColumnNames(selected);
        if (duplicateSheet is not null)
        {
            return $"A planilha '{duplicateSheet}' tem colunas com nomes repetidos. Corrija antes de aplicar.";
        }

        return null;
    }

    private static string? FindSheetWithDuplicateColumnNames(IEnumerable<SheetEditState> sheets) =>
        sheets
            .FirstOrDefault(sheet => sheet.Columns
                .Where(c => c.Include)
                .Select(c => c.Name)
                .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() > 1))
            ?.SheetName;
}
