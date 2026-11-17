namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;

/// <summary>
/// Names the directory that holds the per-machine settings (regex rules, blacklists), so the handlers
/// that read and write them can be pointed at a different directory in tests.
/// </summary>
public interface ISettingsPathProvider
{
    /// <summary>The settings directory. It is not guaranteed to exist.</summary>
    string GetSettingsDirectory();
}
