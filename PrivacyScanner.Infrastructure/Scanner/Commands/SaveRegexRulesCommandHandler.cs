using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

/// <summary>
/// Persists the full list of regex rules to disk as JSON.
/// </summary>
public class SaveRegexRulesCommandHandler(ISettingsPathProvider settingsPathProvider) : IRequestHandler<SaveRegexRulesCommand>
{
    public async ValueTask<Unit> Handle(SaveRegexRulesCommand request, CancellationToken cancellationToken)
    {
        var path = settingsPathProvider.GetSettingsDirectory();

        Directory.CreateDirectory(path);

        path = Path.Combine(path, "rules.json");


        var json = JsonSerializer.Serialize(request.RegexRuleDtos, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(path, json);

        return Unit.Value;
    }
}
