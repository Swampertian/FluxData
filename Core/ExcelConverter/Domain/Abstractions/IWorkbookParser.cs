using DocsData.ExcelConverter.Models;

namespace DocsData.ExcelConverter.Abstractions;

public interface IWorkbookParser
{
    WorkbookData Parse(byte[] content, string fileName);
}
