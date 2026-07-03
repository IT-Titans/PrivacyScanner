using ITTitans.PrivacyScanner.Model;

namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;

/// <summary>The currently saved directory and file-extension blacklists.</summary>
public class GetAllBlacklistItemsQueryResult
{
    public required List<DirectoryBlacklistItemDto> DirectoryBlacklistItems { get; init; }

    public required List<FileExtensionBlacklistItemDto> FileExtensionBlacklistItems { get; init; }
}
