using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;

/// <summary>Retrieves the currently saved directory and file-extension blacklists.</summary>
public class GetAllBlacklistItemsQuery : IRequest<GetAllBlacklistItemsQueryResult>;
