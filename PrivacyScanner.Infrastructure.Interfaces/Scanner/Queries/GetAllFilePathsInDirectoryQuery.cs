using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;

public class GetAllFilePathsInDirectoryQuery : IRequest<GetAllFilePathsInDirectoryQueryResult>
{
    public required DirectoryInfo RootDirectory { get; init; }

    public required List<DirectoryBlacklistItemDto> DirectoryBlacklistItems { get; init; }

    public required List<FileExtensionBlacklistItemDto> FileExtensionBlacklistItems { get; init; }
}
