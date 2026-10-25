using DocsData.ExcelConverter.Models;

namespace DocsData.ExcelConverter.Abstractions;

public interface IWorkbookTransformer
{
    WorkbookData Transform(WorkbookData workbook, WorkbookTransformRequest request);
}
