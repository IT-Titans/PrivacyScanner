using ITTitans.PrivacyScanner.Model;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Services;

/// <summary>
/// Provides default regex rules for detecting personal data (GDPR-relevant).
/// These rules are available by default when the application starts.
/// </summary>
public static class DefaultRegexRulesProvider
{
    /// <summary>
    /// Gets the list of default regex rules for detecting personal data.
    /// </summary>
    public static List<RegexRuleDto> GetDefaultRules()
    {
        return
        [
            // E-Mail-Adresse
            new RegexRuleDto
            {
                RuleId = new Guid("00000001-0000-0000-0000-000000000001"),
                RuleName = "E-Mail-Adresse",
                Rule = @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}"
            },

            // IBAN
            new RegexRuleDto
            {
                RuleId = new Guid("00000001-0000-0000-0000-000000000002"),
                RuleName = "IBAN",
                Rule = @"\b[A-Z]{2}[0-9]{2}[ ]?[0-9]{4}[ ]?[0-9]{4}[ ]?[0-9]{4}[ ]?[0-9]{0,4}\b"
            },

            // Telefonnummer (DE)
            new RegexRuleDto
            {
                RuleId = new Guid("00000001-0000-0000-0000-000000000003"),
                RuleName = "Telefonnummer (DE)",
                Rule = @"(\+49[ -]?[1-9][0-9]{1,4}[ -]?[0-9]{3,12})|(\(?0[1-9][0-9]{1,4}\)?[ -]?[0-9]{3,12})"
            },

            // Postleitzahl (DE)
            new RegexRuleDto
            {
                RuleId = new Guid("00000001-0000-0000-0000-000000000004"),
                RuleName = "Postleitzahl (DE)",
                Rule = @"\b\d{5}\b"
            },

            // Personalausweisnummer (DE)
            new RegexRuleDto
            {
                RuleId = new Guid("00000001-0000-0000-0000-000000000005"),
                RuleName = "Personalausweisnummer (DE)",
                Rule = @"\b[A-Z0-9]{9}\b"
            },

            // Kreditkartennummer (Visa, Mastercard, AMEX, Discover)
            new RegexRuleDto
            {
                RuleId = new Guid("00000001-0000-0000-0000-000000000006"),
                RuleName = "Kreditkartennummer",
                Rule = @"\b(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|3[47][0-9]{13}|6(?:011|5[0-9]{2})[0-9]{12})\b"
            },

            // Datum (dd.mm.yyyy oder yyyy-mm-dd)
            new RegexRuleDto
            {
                RuleId = new Guid("00000001-0000-0000-0000-000000000007"),
                RuleName = "Datum (dd.mm.yyyy / yyyy-mm-dd)",
                Rule = @"\b(0?[1-9]|[12][0-9]|3[01])[.\-/](0?[1-9]|1[0-2])[.\-/]\d{2,4}\b|\b(19|20)\d{2}-(0?[1-9]|1[0-2])-(0?[1-9]|[12][0-9]|3[01])\b"
            },

            // Adresse (vereinfachte DE-Straße + Hausnummer)
            new RegexRuleDto
            {
                RuleId = new Guid("00000001-0000-0000-0000-000000000008"),
                RuleName = "Adresse (Straße + Hausnummer)",
                Rule = @"\b[A-ZÄÖÜa-zäöüß]+(?:\s[A-ZÄÖÜa-zäöüß]+)*\s\d{1,4}[a-zA-Z]?\b"
            }
        ];
    }

    /// <summary>
    /// Checks if a rule ID belongs to a default rule.
    /// </summary>
    public static bool IsDefaultRule(Guid ruleId)
    {
        return GetDefaultRules().Any(r => r.RuleId == ruleId);
    }
}
