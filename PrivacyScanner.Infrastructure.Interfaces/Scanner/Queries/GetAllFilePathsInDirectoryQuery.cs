using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;

/// <summary>Enumerates all file paths under a root directory that survive the given blacklists.</summary>
public class GetAllFilePathsInDirectoryQuery : IRequest<GetAllFilePathsInDirectoryQueryResult>
{
    public required DirectoryInfo RootDirectory { get; init; }

    public required List<DirectoryBlacklistItemDto> DirectoryBlacklistItems { get; init; }

    public required List<FileExtensionBlacklistItemDto> FileExtensionBlacklistItems { get; init; }
}
