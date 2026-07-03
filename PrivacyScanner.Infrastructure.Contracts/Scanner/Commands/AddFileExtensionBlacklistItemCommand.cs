using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;

/// <summary>Adds a file extension to the blacklist of extensions excluded from scans.</summary>
public class AddFileExtensionBlacklistItemCommand : IRequest
{
    public required string Extension { get; init; }
}
