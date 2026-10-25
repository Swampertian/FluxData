using System.Globalization;

namespace DocsData.ExcelConverter.Transform;

// Converts one cell's raw CLR value (double/DateTime/bool/string/null, as produced by
// ExcelWorkbookParser) to a user-chosen target type. Returns null whenever the value can't be
// represented as the target type — the caller nulls out the cell rather than failing the sheet.
internal static class CellTypeConverter
{
    private static readonly string[] TrueValues = ["true", "1", "sim", "yes"];
    private static readonly string[] FalseValues = ["false", "0", "não", "nao", "no"];

    public static object? Convert(object? value, Type targetType)
    {
        if (value is null)
        {
            return null;
        }

        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (targetType == typeof(string))
        {
            return value switch
            {
                DateTime dateTime => dateTime.ToString("s", CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        if (value is double number)
        {
            if (targetType == typeof(bool))
            {
                return number switch { 1 => true, 0 => false, _ => null };
            }

            // Excel stores dates as OLE Automation serials; a number column without a date format
            // still carries them.
            if (targetType == typeof(DateTime))
            {
                return number is >= -657435.0 and < 2958466.0 ? DateTime.FromOADate(number) : null;
            }

            return null;
        }

        if (value is bool flag && targetType == typeof(double))
        {
            return flag ? 1d : 0d;
        }

        if (value is not string text)
        {
            return null;
        }

        if (targetType == typeof(double))
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;
        }

        if (targetType == typeof(DateTime))
        {
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : null;
        }

        if (targetType == typeof(bool))
        {
            var normalized = text.Trim();
            if (TrueValues.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            if (FalseValues.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
            return null;
        }

        return null;
    }
}
