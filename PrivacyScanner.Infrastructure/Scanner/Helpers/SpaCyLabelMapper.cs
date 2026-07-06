using ITTitans.PrivacyScanner.Model;

namespace ITTitans.PrivacyScanner.Infrastructure.Scanner.Helpers;

/// <summary>
/// Maps raw spaCy entity label strings to the coarser <see cref="SpaCyLabel"/> enum.
/// </summary>
public static class SpaCyLabelMapper
{
    public static SpaCyLabel MapToEnum(string? label)
    {
        return label?.ToUpperInvariant() switch
        {
            "PER" or "PERSON" => SpaCyLabel.Per,
            "LOC" or "LOCATION" or "GPE" => SpaCyLabel.Loc,
            "ORG" or "ORGANIZATION" => SpaCyLabel.Org,
            "MISC" => SpaCyLabel.Misc,
            _ => SpaCyLabel.Unknown
        };
    }
}
