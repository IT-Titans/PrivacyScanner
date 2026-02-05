namespace ITTitans.PrivacyScanner.Model;

public class ScanResultDto
{
    public required FileInfo FilePath;


    public required IReadOnlyList<ScanWarningDto> Warnings { get; init; }
}