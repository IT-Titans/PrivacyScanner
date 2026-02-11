using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

public class SaveDirectoryBlacklistItemsStatesCommand : IRequest
{
    public required List<DirectoryBlacklistItemDto> DirectoryBlacklistItems { get; init; }
}