using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

/// <summary>Runs a full directory scan with the given rules and blacklists, reporting progress via events.</summary>
public class ProcessScanCommand : IRequest
{
    public required DirectoryInfo RootDirectory { get; init; }

    public required List<RegexRuleDto>? RegexRuleList { get; init; }

    public required List<DirectoryBlacklistItemDto> DirectoryBlacklistItems { get; init; }

    public required List<FileExtensionBlacklistItemDto> FileExtensionBlacklistItems { get; init; }

    public bool UseSpacy { get; init; }
}
