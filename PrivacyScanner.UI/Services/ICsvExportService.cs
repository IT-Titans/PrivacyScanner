using ITTitans.PrivacyScanner.UI.ViewModels;

namespace ITTitans.PrivacyScanner.UI.Services;

public interface ICsvExportService
{
    void InitializeExport(string filePath);
    void WriteEntry(LogEntryViewModel entry);
    void Close();
    bool IsExportActive { get; }
}
