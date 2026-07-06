using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;
using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;

/// <summary>
/// Adds a directory name to the blacklist and persists the updated list.
/// </summary>
public class AddDirectoryBlacklistItemCommandHandler(IMediator mediator) : IRequestHandler<AddDirectoryBlacklistItemCommand>
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
