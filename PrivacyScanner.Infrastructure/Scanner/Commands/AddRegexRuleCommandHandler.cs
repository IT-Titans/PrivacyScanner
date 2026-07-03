using System.Text.Json;
using System.Text.RegularExpressions;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;
using ITTitans.PrivacyScanner.Model;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
/// <summary>
/// Adds one Regex to your list
/// </summary>
/// <param name="mediator"></param>
public class AddRegexRuleCommandHandler(IMediator mediator, ILogger<AddRegexRuleCommandHandler> logger) : IRequestHandler<AddRegexRuleCommand, AddRegexRuleCommandResult>
{
    public async ValueTask<AddRegexRuleCommandResult> Handle(AddRegexRuleCommand request, CancellationToken cancellationToken)
    {
        if (!IsValidRegex(request.Rule))
        {
            return AddRegexRuleCommandResult.Failure("Der eingegebene Regex ist ungültig");
        }

        var currentRegexDtos = await mediator.Send(new GetAllRegexRulesQuery { });

        currentRegexDtos.Rules.Add(new RegexRuleDto
        {
            RuleName = request.RuleName,
            Rule = request.Rule,
            RuleId = Guid.NewGuid()
        });

        await mediator.Send(new SaveRegexRulesCommand { RegexRuleDtos = currentRegexDtos.Rules });

        return AddRegexRuleCommandResult.Success();
    }

    private bool IsValidRegex(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        try
        {
            _ = Regex.Match("Test", pattern);
        }
        catch (ArgumentException exception)
        {
            logger.LogError(exception, $"The regex \"{pattern}\" is invalid");
            return false;
        }

        return true;
    }
}
