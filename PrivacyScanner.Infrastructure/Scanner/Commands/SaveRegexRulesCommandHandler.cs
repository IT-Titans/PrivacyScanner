using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

public class SaveRegexRulesCommandHandler : IRequestHandler<SaveRegexRulesCommand>
{
    public async ValueTask<Unit> Handle(SaveRegexRulesCommand request, CancellationToken cancellationToken)
    {
        var commonPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var path = Path.Combine(commonPath, "PrivacyScanner");

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