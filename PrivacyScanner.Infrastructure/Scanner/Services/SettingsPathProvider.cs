using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;

/// <summary>
/// Default <see cref="ISettingsPathProvider"/> implementation: <c>%ProgramData%\PrivacyScanner</c>.
/// </summary>
public class SettingsPathProvider : ISettingsPathProvider
{
    public string GetSettingsDirectory()
    {
        var commonPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        return Path.Combine(commonPath, "PrivacyScanner");
    }
}
