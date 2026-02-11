using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

public class AddDirectoryBlacklistItemCommandHandler(IMediator mediator, ILogger<AddDirectoryBlacklistItemCommandHandler> logger) : IRequestHandler<AddDirectoryBlacklistItemCommand>
{
    public async ValueTask<Unit> Handle(AddDirectoryBlacklistItemCommand request, CancellationToken cancellationToken)
    {
        var currentBlacklistItems = await mediator.Send(new GetAllBlacklistItemsQuery());

        currentBlacklistItems.DirectoryBlacklistItems.Add(new DirectoryBlacklistItemDto()
        {
            DirectoryName = request.DirectoryName,
        });

        await mediator.Send(new SaveDirectoryBlacklistItemsStatesCommand()
        {
            DirectoryBlacklistItems = currentBlacklistItems.DirectoryBlacklistItems.DistinctBy(i => i.DirectoryName).ToList(),
        });

        return Unit.Value;
    }
}