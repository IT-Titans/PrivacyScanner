using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

/// <summary>
/// Persists the file extension blacklist to disk as JSON.
/// </summary>
public class SaveFileExtensionBlacklistItemsStatesCommandHandler : IRequestHandler<SaveFileExtensionBlacklistItemsStatesCommand>
{
    public async ValueTask<Unit> Handle(SaveFileExtensionBlacklistItemsStatesCommand request, CancellationToken cancellationToken)
    {
        var commonPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var path = Path.Combine(commonPath, "PrivacyScanner");

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
