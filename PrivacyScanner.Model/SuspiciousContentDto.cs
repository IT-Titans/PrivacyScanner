namespace ITTitans.PrivacyScanner.Model;

public class SuspiciousContentDto
{
    public required string MatchText { get; init; }
    public required string? PrevLine { get; init; }
    public required string HitLine { get; init; }
    public required string? NextLine { get; init; }
}