using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Queries;

/// <summary>
/// Loads the directory and file extension blacklists from disk, falling back to the built-in defaults.
/// </summary>
public class GetAllBlacklistItemsQueryHandler(ILogger<GetAllBlacklistItemsQueryHandler> logger) : IRequestHandler<GetAllBlacklistItemsQuery, GetAllBlacklistItemsQueryResult>
{
    public async ValueTask<GetAllBlacklistItemsQueryResult> Handle(GetAllBlacklistItemsQuery request, CancellationToken cancellationToken)
    {
        var commonPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        var directoryItemsStorePath = Path.Combine(commonPath, "PrivacyScanner", "directoryBlacklists.json");
        var directoryBlacklistItems = await LoadDirectoryBlacklistItemsAsync(directoryItemsStorePath);

        var fileExtensionsItemsStorePath = Path.Combine(commonPath, "PrivacyScanner", "fileExtensionBlacklists.json");
        var fileExtensionsBlacklistItems = await LoadFileExtensionBlacklistItemsAsync(fileExtensionsItemsStorePath);

        return await new ValueTask<GetAllBlacklistItemsQueryResult>(new GetAllBlacklistItemsQueryResult()
        {
            DirectoryBlacklistItems = directoryBlacklistItems,
            FileExtensionBlacklistItems = fileExtensionsBlacklistItems,
        });
    }

    private async Task<List<DirectoryBlacklistItemDto>> LoadDirectoryBlacklistItemsAsync(string path)
    {
        if (!File.Exists(path))
        {
            return DefaultBlacklistItemsProvider.GetDefaultDirectoryItems();
        }

        try
        {
            return await LoadBlacklistItemsAsync<DirectoryBlacklistItemDto>(path);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Konnte die gespeicherte Verzeichnis-Blacklist unter {Path} nicht laden, es werden die Standardwerte verwendet", path);
            return DefaultBlacklistItemsProvider.GetDefaultDirectoryItems();
        }
    }

    private async Task<List<FileExtensionBlacklistItemDto>> LoadFileExtensionBlacklistItemsAsync(string path)
    {
        if (!File.Exists(path))
        {
            return DefaultBlacklistItemsProvider.GetDefaultFileExtensionItems();
        }

        try
        {
            return await LoadBlacklistItemsAsync<FileExtensionBlacklistItemDto>(path);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Konnte die gespeicherte Dateiendungs-Blacklist unter {Path} nicht laden, es werden die Standardwerte verwendet", path);
            return DefaultBlacklistItemsProvider.GetDefaultFileExtensionItems();
        }
    }

    private static async Task<List<TBlacklistItem>> LoadBlacklistItemsAsync<TBlacklistItem>(string path)
    {
        var json = await File.ReadAllTextAsync(path);

        return JsonSerializer.Deserialize<List<TBlacklistItem>>(json)
               ?? new List<TBlacklistItem>();
    }
}
