using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Services;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Helpers;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;

public class ProcessService : IProcessService
{
    public Task<(int ExitCode, string Output, string Error)> RunCommandAsync(string fileName, string arguments)
    {
        return ProcessHelper.RunCommandAsync(fileName, arguments);
    }
}
