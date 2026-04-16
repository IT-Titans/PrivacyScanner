using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

public class AddFileExtensionBlacklistItemCommand : IRequest
{
    public required string Extension { get; init; }
}