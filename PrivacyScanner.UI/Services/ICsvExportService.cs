using ITTitans.PrivacyScanner.UI.ViewModels;

namespace ITTitans.PrivacyScanner.UI.Services;

/// <summary>Exports scan findings to a CSV file for the duration of one scan run.</summary>
public interface ICsvExportService
{
    void InitializeExport(string filePath);
    void WriteEntry(LogEntryViewModel entry);
    void Close();
    bool IsExportActive { get; }
}
