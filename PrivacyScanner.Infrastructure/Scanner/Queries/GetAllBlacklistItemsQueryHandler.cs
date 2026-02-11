using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;
using ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using System.Text.Json;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Queries;

public class GetAllBlacklistItemsQueryHandler : IRequestHandler<GetAllBlacklistItemsQuery, GetAllBlacklistItemsQueryResult>
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

    private static async Task<List<DirectoryBlacklistItemDto>> LoadDirectoryBlacklistItemsAsync(string path)
    {
        if (!File.Exists(path))
        {
            return DefaultBlacklistItemsProvider.GetDefaultDirectoryItems();
        }

        return await LoadBlacklistItemsAsync<DirectoryBlacklistItemDto>(path);
    }

    private static async Task<List<FileExtensionBlacklistItemDto>> LoadFileExtensionBlacklistItemsAsync(string path)
    {
        if (!File.Exists(path))
        {
            return DefaultBlacklistItemsProvider.GetDefaultFileExtensionItems();
        }

        return await LoadBlacklistItemsAsync<FileExtensionBlacklistItemDto>(path);
    }

    private static async Task<List<TBlacklistItem>> LoadBlacklistItemsAsync<TBlacklistItem>(string path)
    {
        var json = await File.ReadAllTextAsync(path);

        return JsonSerializer.Deserialize<List<TBlacklistItem>>(json)
               ?? new List<TBlacklistItem>();
    }
}