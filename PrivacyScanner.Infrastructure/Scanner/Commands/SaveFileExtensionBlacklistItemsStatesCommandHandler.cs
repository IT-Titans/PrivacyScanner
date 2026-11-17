using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Services;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

/// <summary>
/// Persists the file extension blacklist to disk as JSON.
/// </summary>
public class SaveFileExtensionBlacklistItemsStatesCommandHandler(ISettingsPathProvider settingsPathProvider) : IRequestHandler<SaveFileExtensionBlacklistItemsStatesCommand>
{
    public async ValueTask<Unit> Handle(SaveFileExtensionBlacklistItemsStatesCommand request, CancellationToken cancellationToken)
    {
        var path = settingsPathProvider.GetSettingsDirectory();

        Directory.CreateDirectory(path);

        path = Path.Combine(path, "fileExtensionBlacklists.json");

        var json = JsonSerializer.Serialize(request.FileExtensionBlacklistItems, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(path, json, cancellationToken);

        return Unit.Value;
    }
}
