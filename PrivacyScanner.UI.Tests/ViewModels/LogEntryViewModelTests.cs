using ITTitans.PrivacyScanner.Model;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.ViewModels;

/// <summary>
/// Covers how a finding is split around the match and shortened for display.
/// </summary>
public class LogEntryViewModelTests
{
    private static SuspiciousContentDto Content(string hit, string? prev = "prev", string? next = "next") =>
        new() { MatchText = "m", PrevLine = prev, HitLine = hit, NextLine = next };

    private static List<string?> Track(LogEntryViewModel vm)
    {
        var names = new List<string?>();
        vm.PropertyChanged += (_, e) => names.Add(e.PropertyName);
        return names;
    }

    [Fact]
    public void WithoutContent_AllTextPropertiesAreEmpty()
    {
        var vm = new LogEntryViewModel();

        Assert.Equal(string.Empty, vm.PrevLine);
        Assert.Equal(string.Empty, vm.HitLine);
        Assert.Equal(string.Empty, vm.NextLine);
        Assert.Equal(string.Empty, vm.BeforeTarget);
        Assert.Equal(string.Empty, vm.TargetContent);
        Assert.Equal(string.Empty, vm.AfterTarget);
        Assert.False(vm.IsTextTruncated);
    }

    [Fact]
    public void ContentWithNullNeighbours_ExposesEmptyStrings()
    {
        var vm = new LogEntryViewModel { SuspiciousContent = Content("hit", null, null) };

        Assert.Equal(string.Empty, vm.PrevLine);
        Assert.Equal("hit", vm.HitLine);
        Assert.Equal(string.Empty, vm.NextLine);
    }

    [Fact]
    public void HitLineIsSplitIntoBeforeTargetAndAfter()
    {
        var vm = new LogEntryViewModel
        {
            SuspiciousContent = Content("abcdefghij", "above", "below"),
            Position = 3,
            End = 6,
        };

        Assert.Equal("above", vm.PrevLine);
        Assert.Equal("below", vm.NextLine);
        Assert.Equal("abc", vm.BeforeTarget);
        Assert.Equal("def", vm.TargetContent);
        Assert.Equal("ghij", vm.AfterTarget);
    }

    [Fact]
    public void PositionAtStart_LeavesBeforeTargetEmpty()
    {
        var vm = new LogEntryViewModel { SuspiciousContent = Content("abcdef"), Position = 0, End = 2 };

        Assert.Equal(string.Empty, vm.BeforeTarget);
        Assert.Equal("ab", vm.TargetContent);
        Assert.Equal("cdef", vm.AfterTarget);
    }

    [Fact]
    public void EndBeyondLine_ClampsTheTargetAndLeavesAfterEmpty()
    {
        var vm = new LogEntryViewModel { SuspiciousContent = Content("abcdef"), Position = 4, End = 50 };

        Assert.Equal("abcd", vm.BeforeTarget);
        Assert.Equal("ef", vm.TargetContent);
        Assert.Equal(string.Empty, vm.AfterTarget);
    }

    [Fact]
    public void PositionBeyondLine_YieldsEmptyTargetAndAfter()
    {
        var vm = new LogEntryViewModel { SuspiciousContent = Content("abc"), Position = 10, End = 12 };

        Assert.Equal(string.Empty, vm.TargetContent);
        Assert.Equal(string.Empty, vm.AfterTarget);
    }

    [Fact]
    public void EndBeforePosition_YieldsEmptyTarget()
    {
        var vm = new LogEntryViewModel { SuspiciousContent = Content("abcdef"), Position = 4, End = 2 };

        Assert.Equal(string.Empty, vm.TargetContent);
    }

    [Fact]
    public void ZeroPositionAndEnd_PutsTheWholeLineAfterTheTarget()
    {
        var vm = new LogEntryViewModel { SuspiciousContent = Content("abc") };

        Assert.Equal(string.Empty, vm.BeforeTarget);
        Assert.Equal(string.Empty, vm.TargetContent);
        Assert.Equal("abc", vm.AfterTarget);
    }

    [Fact]
    public void LongLines_AreCutAtFiveHundredCharactersWithMarker()
    {
        var longLine = new string('x', 501);
        var vm = new LogEntryViewModel { SuspiciousContent = Content("h", longLine, longLine) };

        Assert.True(vm.IsTextTruncated);
        Assert.Equal(new string('x', 500) + "... [gekürzt]", vm.DisplayPrevLine);
        Assert.Equal(new string('x', 500) + "... [gekürzt]", vm.DisplayNextLine);
        Assert.Equal(longLine, vm.PrevLine);
    }

    [Fact]
    public void LineOfExactlyFiveHundredCharacters_IsNotShortened()
    {
        var line = new string('x', 500);
        var vm = new LogEntryViewModel { SuspiciousContent = Content(line) };

        Assert.False(vm.IsTextTruncated);
        Assert.Equal(line, vm.DisplayAfterTarget);
    }

    [Fact]
    public void LongHitLine_MarksTruncationAndShortensTargetPartsIndividually()
    {
        var hit = new string('a', 600);
        var vm = new LogEntryViewModel { SuspiciousContent = Content(hit), Position = 0, End = 550 };

        Assert.True(vm.IsTextTruncated);
        Assert.Equal(string.Empty, vm.DisplayBeforeTarget);
        Assert.Equal(new string('a', 500) + "... [gekürzt]", vm.DisplayTargetContent);
        Assert.Equal(new string('a', 50), vm.DisplayAfterTarget);
    }

    [Fact]
    public void ShowFullText_DisablesShorteningAndRaisesDisplayChanges()
    {
        var longLine = new string('x', 700);
        var vm = new LogEntryViewModel { SuspiciousContent = Content("h", longLine) };
        var changed = Track(vm);

        vm.ShowFullText = true;

        Assert.Equal(longLine, vm.DisplayPrevLine);
        Assert.Contains(nameof(LogEntryViewModel.ShowFullText), changed);
        Assert.Contains(nameof(LogEntryViewModel.DisplayPrevLine), changed);
        Assert.Contains(nameof(LogEntryViewModel.DisplayNextLine), changed);
        Assert.Contains(nameof(LogEntryViewModel.DisplayBeforeTarget), changed);
        Assert.Contains(nameof(LogEntryViewModel.DisplayTargetContent), changed);
        Assert.Contains(nameof(LogEntryViewModel.DisplayAfterTarget), changed);
    }

    [Fact]
    public void ShowFullText_SameValueRaisesNothing()
    {
        var vm = new LogEntryViewModel();
        var changed = Track(vm);

        vm.ShowFullText = false;

        Assert.Empty(changed);
    }

    [Fact]
    public void SettingContent_RaisesAllDependentProperties()
    {
        var vm = new LogEntryViewModel();
        var changed = Track(vm);

        vm.SuspiciousContent = Content("hit");

        foreach (var name in new[]
        {
            nameof(LogEntryViewModel.SuspiciousContent), nameof(LogEntryViewModel.BeforeTarget),
            nameof(LogEntryViewModel.TargetContent), nameof(LogEntryViewModel.AfterTarget),
            nameof(LogEntryViewModel.IsTextTruncated), nameof(LogEntryViewModel.PrevLine),
            nameof(LogEntryViewModel.HitLine), nameof(LogEntryViewModel.NextLine),
            nameof(LogEntryViewModel.DisplayPrevLine),
        })
        {
            Assert.Contains(name, changed);
        }
    }

    [Fact]
    public void SettingPositionAndEnd_RaisesPartChanges()
    {
        var vm = new LogEntryViewModel { SuspiciousContent = Content("abcdef") };
        var changed = Track(vm);

        vm.Position = 1;
        vm.End = 3;

        Assert.Contains(nameof(LogEntryViewModel.Position), changed);
        Assert.Contains(nameof(LogEntryViewModel.End), changed);
        Assert.Contains(nameof(LogEntryViewModel.TargetContent), changed);
        Assert.Equal("bc", vm.TargetContent);
    }

    [Fact]
    public void PlainProperties_StoreValuesAndRaiseOnChangeOnly()
    {
        var vm = new LogEntryViewModel();
        var changed = Track(vm);

        vm.FilePath = @"C:\a.txt";
        vm.Line = 7;
        vm.Type = ScanWarningType.Rule;
        vm.SpacyLabel = SpaCyLabel.Per;
        vm.RuleName = "Email";
        var countAfterFirstSet = changed.Count;
        vm.FilePath = @"C:\a.txt";
        vm.Line = 7;

        Assert.Equal(@"C:\a.txt", vm.FilePath);
        Assert.Equal(7, vm.Line);
        Assert.Equal(ScanWarningType.Rule, vm.Type);
        Assert.Equal(SpaCyLabel.Per, vm.SpacyLabel);
        Assert.Equal("Email", vm.RuleName);
        Assert.Equal(5, countAfterFirstSet);
        Assert.Equal(countAfterFirstSet, changed.Count);
    }
}
