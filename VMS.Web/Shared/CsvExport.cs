using System.Text;

namespace VMS.Web.Shared;

public static class CsvExport
{
    public static string BuildDataUri(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', headers.Select(EscapeField)));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',', row.Select(EscapeField)));
        }

        var encoded = Uri.EscapeDataString(builder.ToString());
        return $"data:text/csv;charset=utf-8,{encoded}";
    }

    private static string EscapeField(string field)
    {
        field ??= string.Empty;

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
