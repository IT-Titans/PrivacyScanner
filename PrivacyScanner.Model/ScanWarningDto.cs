namespace ITTitans.PrivacyScanner.Model;

/// <summary>A single detection hit: its location in the file, the matched content, and its source (regex rule or SpaCy label).</summary>
public class ScanWarningDto
{
    public required int Line { get; init; }

    public required int Start { get; init; }

    public required int End { get; init; }

    public required SuspiciousContentDto SuspiciousContent { get; init; }

    public required ScanWarningType Type { get; init; }

    public string? RuleName { get; set; }

    public required Guid? RuleId { get; init; }

    public required SpaCyLabel? SpacyLabel { get; init; }
}
