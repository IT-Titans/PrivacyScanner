using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Queries;

/// <summary>Retrieves the currently saved directory and file-extension blacklists.</summary>
public class GetAllBlacklistItemsQuery : IRequest<GetAllBlacklistItemsQueryResult>;
