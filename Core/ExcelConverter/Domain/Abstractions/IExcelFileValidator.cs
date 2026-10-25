namespace DocsData.ExcelConverter.Abstractions;

public interface IExcelFileValidator
{
    bool IsValid(string fileName);
}
