using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;
using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
/// <summary>
/// Deletes One Regex out of your regexes
/// </summary>
/// <param name="mediator"></param>
public class DeleteRegexRuleCommandHandler(IMediator mediator) : IRequestHandler<DeleteRegexRuleCommand>
{
    public async ValueTask<Unit> Handle(DeleteRegexRuleCommand request, CancellationToken cancellationToken)
    {
        var regexRuleDtos = await mediator.Send(new GetAllRegexRulesQuery { });

        var regexToBeRemoved = regexRuleDtos.Rules.FirstOrDefault(rRD => rRD.RuleId == request.RuleId);

        if (regexToBeRemoved == null)
        {
            throw new ArgumentException("This Regex does not exist");
        }

        regexRuleDtos.Rules.Remove(regexToBeRemoved);
        await mediator.Send(new SaveRegexRulesCommand { RegexRuleDtos = regexRuleDtos.Rules });

        return Unit.Value;
    }
}