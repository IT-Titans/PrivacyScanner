namespace ITTitans.PrivacyScanner.Model;

/// <summary>The outcome of scanning a single file, including all warnings found in it.</summary>
public class ScanResultDto
{
    public required FileInfo FilePath;


    public required IReadOnlyList<ScanWarningDto> Warnings { get; init; }
}
