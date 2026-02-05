using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Services;
using Mediator;
using System.Text.RegularExpressions;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Queries;

public class CheckPythonAndSpacyEnvironmentQueryHandler(IProcessService processService)
    : IRequestHandler<CheckPythonAndSpacyEnvironmentQuery, CheckPythonAndSpacyEnvironmentQueryResult>
{
    public async ValueTask<CheckPythonAndSpacyEnvironmentQueryResult> Handle(
        CheckPythonAndSpacyEnvironmentQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var pythonResult = await CheckPythonVersionAsync();
            if (!pythonResult.IsValid) return pythonResult;

            var spacyResult = await CheckSpacyInstallationAsync();
            if (!spacyResult.IsValid) return spacyResult;

            var spacyModelResult = await CheckSpacyGermanModelAsync();
            if (!spacyModelResult.IsValid) return spacyModelResult;

            return CheckPythonAndSpacyEnvironmentQueryResult.Success();
        }
        catch (Exception ex)
        {
            return CheckPythonAndSpacyEnvironmentQueryResult.Failure(
                "Ein unerwarteter Fehler ist bei der Umgebungsprüfung aufgetreten.",
                ex.Message);
        }
    }

    private async Task<CheckPythonAndSpacyEnvironmentQueryResult> CheckPythonVersionAsync()
    {
        var (exitCode, output, error) = await processService.RunCommandAsync("py", "--version");

        if (exitCode != 0)
        {
            return CheckPythonAndSpacyEnvironmentQueryResult.Failure(
                "Python 3.12 konnte nicht gefunden werden. Bitte installieren Sie Python 3.12.",
                $"ExitCode: {exitCode}, Error: {error}");
        }

        string combinedOutput = (output + error).Trim();
        var versionMatch = Regex.Match(combinedOutput, @"Python 3\.12\.\d+");
        if (!versionMatch.Success)
        {
            return CheckPythonAndSpacyEnvironmentQueryResult.Failure(
                "Die installierte Python-Version konnte nicht korrekt erkannt werden.",
                $"Output: {combinedOutput}");
        }

        return CheckPythonAndSpacyEnvironmentQueryResult.Success();
    }

    private async Task<CheckPythonAndSpacyEnvironmentQueryResult> CheckSpacyInstallationAsync()
    {
        var (exitCode, _, error) =
            await processService.RunCommandAsync("py", "-3.12 -m pip show spacy");

        if (exitCode != 0)
        {
            return CheckPythonAndSpacyEnvironmentQueryResult.Failure(
                "spaCy ist nicht installiert. Bitte führen Sie 'py -3.12 -m pip install spacy' aus.",
                $"ExitCode: {exitCode}, Error: {error}");
        }

        return CheckPythonAndSpacyEnvironmentQueryResult.Success();
    }

    private async Task<CheckPythonAndSpacyEnvironmentQueryResult> CheckSpacyGermanModelAsync()
    {
        var (exitCode, output, error) =
            await processService.RunCommandAsync(
                "py",
                "-3.12 -m spacy info de_core_news_sm");

        if (exitCode != 0)
        {
            return CheckPythonAndSpacyEnvironmentQueryResult.Failure(
                "Das spaCy Sprachmodell 'de_core_news_sm' ist nicht installiert.",
                "Bitte führen Sie 'py -3.12 -m spacy download de_core_news_sm' aus.");
        }

        return CheckPythonAndSpacyEnvironmentQueryResult.Success();
    }
}


