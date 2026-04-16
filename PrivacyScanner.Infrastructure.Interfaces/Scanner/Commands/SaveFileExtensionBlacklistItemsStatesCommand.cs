using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;

public class SaveFileExtensionBlacklistItemsStatesCommand : IRequest
{
    public required List<FileExtensionBlacklistItemDto> FileExtensionBlacklistItems { get; init; }
}