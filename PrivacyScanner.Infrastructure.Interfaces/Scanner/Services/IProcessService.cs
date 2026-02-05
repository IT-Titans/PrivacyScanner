namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Services;

public interface IProcessService
{
    Task<(int ExitCode, string Output, string Error)> RunCommandAsync(string fileName, string arguments);
}
