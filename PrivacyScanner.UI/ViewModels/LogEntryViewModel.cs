using ITTitans.PrivacyScanner.Model;

namespace ITTitans.PrivacyScanner.UI.ViewModels;

public class LogEntryViewModel : ViewModelBase
{
    private string _filePath = string.Empty;
    private int _line;
    private SuspiciousContentDto? _suspiciousContent;
    private int _position;
    private int _end;
    private ScanWarningType _type;
    private SpaCyLabel? _spacyLabel;
    private string? _ruleName;
    private bool _showFullText;
    private const int MaxPreviewLength = 500;

    public bool ShowFullText
    {
        get => _showFullText;
        set
        {
            if (SetProperty(ref _showFullText, value))
            {
                RefreshAllTextProperties();
            }
        }
    }

    public bool IsTextTruncated => 
        (SuspiciousContent?.PrevLine?.Length > MaxPreviewLength) ||
        (SuspiciousContent?.HitLine?.Length > MaxPreviewLength) ||
        (SuspiciousContent?.NextLine?.Length > MaxPreviewLength);

    public string DisplayPrevLine => GetDisplayText(PrevLine);
    public string DisplayNextLine => GetDisplayText(NextLine);
    public string DisplayBeforeTarget => GetDisplayText(BeforeTarget);
    public string DisplayTargetContent => GetDisplayText(TargetContent);
    public string DisplayAfterTarget => GetDisplayText(AfterTarget);

    private string GetDisplayText(string original)
    {
        if (ShowFullText || string.IsNullOrEmpty(original) || original.Length <= MaxPreviewLength)
            return original;

        return original.Substring(0, MaxPreviewLength) + "... [gekürzt]";
    }

    private void RefreshAllTextProperties()
    {
        OnPropertyChanged(nameof(DisplayPrevLine));
        OnPropertyChanged(nameof(DisplayNextLine));
        OnPropertyChanged(nameof(DisplayBeforeTarget));
        OnPropertyChanged(nameof(DisplayTargetContent));
        OnPropertyChanged(nameof(DisplayAfterTarget));
    }

    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    public int Line
    {
        get => _line;
        set => SetProperty(ref _line, value);
    }

    public SuspiciousContentDto? SuspiciousContent
    {
        get => _suspiciousContent;
        set
        {
            if (SetProperty(ref _suspiciousContent, value))
            {
                RefreshParts();
                OnPropertyChanged(nameof(IsTextTruncated));
                RefreshAllTextProperties();
                OnPropertyChanged(nameof(PrevLine));
                OnPropertyChanged(nameof(HitLine));
                OnPropertyChanged(nameof(NextLine));
            }
        }
    }

    public string PrevLine => SuspiciousContent?.PrevLine ?? string.Empty;
    public string HitLine => SuspiciousContent?.HitLine ?? string.Empty;
    public string NextLine => SuspiciousContent?.NextLine ?? string.Empty;

    public int Position
    {
        get => _position;
        set
        {
            if (SetProperty(ref _position, value))
            {
                RefreshParts();
                RefreshAllTextProperties();
            }
        }
    }

    public int End
    {
        get => _end;
        set
        {
            if (SetProperty(ref _end, value))
            {
                RefreshParts();
                RefreshAllTextProperties();
            }
        }
    }

    public string BeforeTarget => SafeSubstring(0, Position);
    public string TargetContent => SafeSubstring(Position, End - Position);
    public string AfterTarget => SafeSubstring(End);

    private void RefreshParts()
    {
        OnPropertyChanged(nameof(BeforeTarget));
        OnPropertyChanged(nameof(TargetContent));
        OnPropertyChanged(nameof(AfterTarget));
    }

    private string SafeSubstring(int start, int? length = null)
    {
        if (string.IsNullOrEmpty(HitLine) || start < 0 || start >= HitLine.Length)
            return start == 0 && length == null ? HitLine : string.Empty;

        if (length == null) return HitLine.Substring(start);

        int safeLength = Math.Min(length.Value, HitLine.Length - start);
        return safeLength > 0 ? HitLine.Substring(start, safeLength) : string.Empty;
    }

    public ScanWarningType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public SpaCyLabel? SpacyLabel
    {
        get => _spacyLabel;
        set => SetProperty(ref _spacyLabel, value);
    }

    public string? RuleName
    {
        get => _ruleName;
        set => SetProperty(ref _ruleName, value);
    }
}
