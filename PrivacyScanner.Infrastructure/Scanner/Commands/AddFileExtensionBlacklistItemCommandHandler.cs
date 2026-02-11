using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

public class AddFileExtensionBlacklistItemCommandHandler(IMediator mediator, ILogger<AddFileExtensionBlacklistItemCommand> logger) : IRequestHandler<AddFileExtensionBlacklistItemCommand>
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