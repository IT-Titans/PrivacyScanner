namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;

/// <summary>Abstracts running an external process and capturing its output, used for the Python/spaCy bridge.</summary>
public interface IProcessService
{
    Task<(int ExitCode, string Output, string Error)> RunCommandAsync(string fileName, string arguments);
}
