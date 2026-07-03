using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;

/// <summary>Persists the full file-extension blacklist, overwriting the previously saved state.</summary>
public class SaveFileExtensionBlacklistItemsStatesCommand : IRequest
{
    public required List<FileExtensionBlacklistItemDto> FileExtensionBlacklistItems { get; init; }
}
