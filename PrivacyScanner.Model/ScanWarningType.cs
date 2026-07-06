namespace ITTitans.PrivacyScanner.Model;

/// <summary>Identifies whether a warning was produced by SpaCy NLP detection or a regex rule.</summary>
public enum ScanWarningType
{
    SpaCy = 1,
    Rule = 2,
}
