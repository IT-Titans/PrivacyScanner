using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

public class AddDirectoryBlacklistItemCommand : IRequest
{
    public required string DirectoryName { get; init; }
}