namespace DocsData.ExcelConverter;

// User-safe pipeline error; InnerException (if any) is diagnostics-only, never shown in the UI.
public sealed class ExcelConverterException : Exception
{
    public ExcelConverterException(string userSafeMessage, Exception? inner = null)
        : base(userSafeMessage, inner)
    {
    }
}
