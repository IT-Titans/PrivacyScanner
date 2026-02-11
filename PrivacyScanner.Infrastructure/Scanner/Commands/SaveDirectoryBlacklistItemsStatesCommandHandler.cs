using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

public class SaveDirectoryBlacklistItemsStatesCommandHandler : IRequestHandler<SaveDirectoryBlacklistItemsStatesCommand>
{
    public async ValueTask<Unit> Handle(SaveDirectoryBlacklistItemsStatesCommand request, CancellationToken cancellationToken)
    {
        var commonPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var path = Path.Combine(commonPath, "PrivacyScanner");

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