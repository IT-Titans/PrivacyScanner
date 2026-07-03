using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

/// <summary>Persists the full directory blacklist, overwriting the previously saved state.</summary>
public class SaveDirectoryBlacklistItemsStatesCommand : IRequest
{
    public required List<DirectoryBlacklistItemDto> DirectoryBlacklistItems { get; init; }
}
