using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

/// <summary>Adds a directory name to the blacklist of directories excluded from scans.</summary>
public class AddDirectoryBlacklistItemCommand : IRequest
{
    public required string DirectoryName { get; init; }
}
