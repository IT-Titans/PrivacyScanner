using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

/// <summary>
/// Adds a file extension to the blacklist and persists the updated list.
/// </summary>
public class AddFileExtensionBlacklistItemCommandHandler(IMediator mediator) : IRequestHandler<AddFileExtensionBlacklistItemCommand>
{
    public async ValueTask<Unit> Handle(AddFileExtensionBlacklistItemCommand request, CancellationToken cancellationToken)
    {
        var currentBlacklistItems = await mediator.Send(new GetAllBlacklistItemsQuery());

        currentBlacklistItems.FileExtensionBlacklistItems.Add(new FileExtensionBlacklistItemDto()
        {
            Extension = request.Extension,
        });

        await mediator.Send(new SaveFileExtensionBlacklistItemsStatesCommand()
        {
            FileExtensionBlacklistItems = currentBlacklistItems.FileExtensionBlacklistItems.DistinctBy(i => i.Extension).ToList(),
        });

        return Unit.Value;
    }
}
