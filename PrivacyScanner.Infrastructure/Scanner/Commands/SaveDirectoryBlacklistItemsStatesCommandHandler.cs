using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

/// <summary>
/// Persists the directory blacklist to disk as JSON.
/// </summary>
public class SaveDirectoryBlacklistItemsStatesCommandHandler(ISettingsPathProvider settingsPathProvider) : IRequestHandler<SaveDirectoryBlacklistItemsStatesCommand>
{
    public async ValueTask<Unit> Handle(SaveDirectoryBlacklistItemsStatesCommand request, CancellationToken cancellationToken)
    {
        var path = settingsPathProvider.GetSettingsDirectory();

        Directory.CreateDirectory(path);

        path = Path.Combine(path, "directoryBlacklists.json");

        var json = JsonSerializer.Serialize(request.DirectoryBlacklistItems, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(path, json, cancellationToken);

        return Unit.Value;
    }
}
