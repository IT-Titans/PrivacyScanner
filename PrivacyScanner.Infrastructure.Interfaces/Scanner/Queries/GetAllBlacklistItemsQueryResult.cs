using ITTitans.PrivacyScanner.Model;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;

public class GetAllBlacklistItemsQueryResult
{
    public required List<DirectoryBlacklistItemDto> DirectoryBlacklistItems { get; init; }

    public required List<FileExtensionBlacklistItemDto> FileExtensionBlacklistItems { get; init; }
}