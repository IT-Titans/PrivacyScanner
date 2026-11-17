using System.IO;
using System.Text;
using ITTitans.PrivacyScanner.UI.ViewModels;

namespace ITTitans.PrivacyScanner.UI.Services;

/// <summary>Writes scan findings to a semicolon-separated CSV file as they are found.</summary>
public class CsvExportService : ICsvExportService, IDisposable
{
    private StreamWriter? _writer;
    private readonly object _lock = new();
    private const string CsvSeparator = ";";

    public bool IsExportActive => _writer != null;

    public void InitializeExport(string filePath)
    {
        lock (_lock)
        {
            Close();

            _writer = new StreamWriter(filePath, false, Encoding.UTF8);
            WriteHeader();
        }
    }

    private void WriteHeader()
    {
        var header = string.Join(CsvSeparator,
            "Typ",
            "Datei",
            "Zeile",
            "Position",
            "Ende",
            "Regelname",
            "SpaCy-Label",
            "Vorherige Zeile",
            "Trefferzeile",
            "Nächste Zeile");
        _writer?.WriteLine(header);
        _writer?.Flush();
    }

    public void WriteEntry(LogEntryViewModel entry)
    {
        lock (_lock)
        {
            if (_writer == null)
                return;

            var line = string.Join(CsvSeparator,
                EscapeCsvField(entry.Type.ToString()),
                EscapeCsvText(entry.FilePath),
                entry.Line.ToString(),
                entry.Position.ToString(),
                entry.End.ToString(),
                EscapeCsvText(entry.RuleName ?? ""),
                EscapeCsvField(entry.SpacyLabel?.ToString() ?? ""),
                EscapeCsvText(entry.PrevLine),
                EscapeCsvText(entry.HitLine),
                EscapeCsvText(entry.NextLine));

            _writer.WriteLine(line);
            _writer.Flush();
        }
    }

    /// <summary>
    /// Escapes a field that carries scanned content or file-system names. A leading
    /// <c>=</c>, <c>+</c>, <c>-</c>, <c>@</c>, tab or carriage return would be run as a formula by
    /// spreadsheet programs (CSV injection, OWASP), so such values get a leading apostrophe.
    /// The apostrophe is added before quoting, so it ends up inside the quotes.
    /// </summary>
    private static string EscapeCsvText(string? field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        if (field[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            field = "'" + field;

        return EscapeCsvField(field);
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        if (field.Contains(CsvSeparator) || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    public void Close()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    public void Dispose()
    {
        Close();
    }
}
