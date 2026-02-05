using System.IO;
using System.Text;
using ITTitans.PrivacyScanner.UI.ViewModels;

namespace ITTitans.PrivacyScanner.UI.Services;

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
            if (_writer == null) return;

            var line = string.Join(CsvSeparator,
                EscapeCsvField(entry.Type.ToString()),
                EscapeCsvField(entry.FilePath),
                entry.Line.ToString(),
                entry.Position.ToString(),
                entry.End.ToString(),
                EscapeCsvField(entry.RuleName ?? ""),
                EscapeCsvField(entry.SpacyLabel?.ToString() ?? ""),
                EscapeCsvField(entry.PrevLine),
                EscapeCsvField(entry.HitLine),
                EscapeCsvField(entry.NextLine));

            _writer.WriteLine(line);
            _writer.Flush();
        }
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
