namespace ITTitans.PrivacyScanner.Model;

/// <summary>The matched text of a warning together with its surrounding line context.</summary>
public class SuspiciousContentDto
{
    public required string MatchText { get; init; }
    public required string? PrevLine { get; init; }
    public required string HitLine { get; init; }
    public required string? NextLine { get; init; }
}
