namespace ITTitans.PrivacyScanner.Model;

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