using System.Text.Json;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Commands;
using ITTitans.PrivacyScanner.Infrastructure.Interfaces.Scanner.Queries;
using ITTitans.PrivacyScanner.Model;
using Mediator;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Commands;
/// <summary>
/// Change one regex out of your regexes
/// </summary>
/// <param name="mediator"></param>
public class EditRegexRuleCommandHandler(IMediator mediator) : IRequestHandler<EditRegexRuleCommand>
{
    public async ValueTask<Unit> Handle(EditRegexRuleCommand request, CancellationToken cancellationToken)
    {
        var regexRuleDtos = await mediator.Send(new GetAllRegexRulesQuery { });
        var regexToEdit = regexRuleDtos.Rules.FirstOrDefault(rRD => rRD.RuleId == request.RuleId);

        if (regexToEdit == null)
        {
            throw new ArgumentException("This Regex does not exist");
        }

        if (request.Rule == null && request.RuleName == null)
        {
            throw new ArgumentException("Missing information what to change");
        }
        else if (request.Rule != null && request.RuleName != null)
        {
            var regexDto = new RegexRuleDto
            {
                RuleName = request.RuleName,
                Rule = request.Rule,
                RuleId = regexToEdit.RuleId
            };

            regexRuleDtos.Rules.Remove(regexToEdit);
            regexRuleDtos.Rules.Add(regexDto);
        }
        else if (request.Rule == null)
        {
            var regexDto = new RegexRuleDto
            {
                RuleName = request.RuleName!,
                Rule = regexToEdit.Rule,
                RuleId = regexToEdit.RuleId
            };

            regexRuleDtos.Rules.Remove(regexToEdit);
            regexRuleDtos.Rules.Add(regexDto);
        }
        else if (request.RuleName == null)
        {
            var regexDto = new RegexRuleDto
            {
                RuleName = regexToEdit.RuleName,
                Rule = request.Rule,
                RuleId = regexToEdit.RuleId
            };

            regexRuleDtos.Rules.Remove(regexToEdit);
            regexRuleDtos.Rules.Add(regexDto);
        }

        await mediator.Send(new SaveRegexRulesCommand { RegexRuleDtos = regexRuleDtos.Rules });

        return Unit.Value;
    }
}