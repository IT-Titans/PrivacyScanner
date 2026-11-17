using System.IO;

using ITTitans.PrivacyScanner.Infrastructure.Contracts.Scanner.Commands;
using ITTitans.PrivacyScanner.Model;
using ITTitans.PrivacyScanner.UI.Tests.TestSupport;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Mediator;

using Moq;

using Xunit;

using static ITTitans.PrivacyScanner.UI.Tests.ViewModels.MainViewModelScanTestData;

namespace ITTitans.PrivacyScanner.UI.Tests.ViewModels;

/// <summary>
/// Covers the results tree of the main view model: how findings become file nodes, how the nodes are
/// paged, and how the details of a file are loaded level by level when a node is expanded.
/// </summary>
public class MainViewModelTreeTests
{
    private const string FileA = @"C:\Daten\a.txt";

    private static async Task<(MainViewModelHarness Harness, FileLogNodeViewModel Node)> WithOneFileAsync(
        string path = FileA,
        int findings = 1)
    {
        var harness = new MainViewModelHarness();
        await harness.ViewModel.Handle(Found(path, findings), default);

        return (harness, Assert.IsType<FileLogNodeViewModel>(Assert.Single(harness.ViewModel.PagedLogTreeNodes)));
    }

    private static void ReturnDetails(MainViewModelHarness harness, params ScanWarningDto[] warnings) =>
        harness.Mediator
            .Setup(m => m.Send(It.IsAny<ScanFileCommand>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<ScanResultDto>(
                new ScanResultDto { FilePath = new FileInfo(FileA), Warnings = warnings }));

    [Fact]
    public async Task AFileWithFindingsBecomesOneNodeWithTheirCount()
    {
        var (_, node) = await WithOneFileAsync(findings: 3);

        Assert.Equal(FileA, node.FullPath);
        Assert.Equal(3, node.WarningCount);
        Assert.Equal($@"{FileA} (3)", node.Title);
    }

    [Fact]
    public async Task ANewFileNodeShowsAPlaceholderChildSoThatItCanBeExpanded()
    {
        var (_, node) = await WithOneFileAsync();

        var child = Assert.IsType<LoadingLogNodeViewModel>(Assert.Single(node.Children));
        Assert.Equal("Lade Details...", child.Title);
        Assert.False(node.IsLoaded);
    }

    [Fact]
    public async Task FurtherFindingsInTheSameFileExtendItsNode()
    {
        var (harness, node) = await WithOneFileAsync(findings: 2);

        await harness.ViewModel.Handle(Found(FileA, 3), default);

        Assert.Same(node, Assert.Single(harness.ViewModel.PagedLogTreeNodes));
        Assert.Equal(5, node.WarningCount);
        Assert.Equal($@"{FileA} (5)", node.Title);
    }

    [Fact]
    public async Task EachFileGetsItsOwnNode()
    {
        var (harness, _) = await WithOneFileAsync();

        await harness.ViewModel.Handle(Found(@"C:\Daten\b.txt", 1), default);

        Assert.Equal(2, harness.ViewModel.PagedLogTreeNodes.Count);
    }

    [Fact]
    public async Task AFileWithoutFindingsGetsNoNode()
    {
        var harness = new MainViewModelHarness();

        await harness.ViewModel.Handle(Found(FileA), default);

        Assert.Empty(harness.ViewModel.PagedLogTreeNodes);
        Assert.Equal(0, harness.ViewModel.TotalWarningsCount);
    }

    [Fact]
    public async Task OnePageHoldsPageSizeNodesAndTheRestIsPaged()
    {
        var harness = new MainViewModelHarness();
        harness.ViewModel.PageSize = 2;

        for (var i = 1; i <= 5; i++)
        {
            await harness.ViewModel.Handle(Found($@"C:\Daten\f{i}.txt", 1), default);
        }

        var vm = harness.ViewModel;
        Assert.Equal(3, vm.TotalPages);
        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal([@"C:\Daten\f1.txt", @"C:\Daten\f2.txt"], FullPaths(vm));
    }

    [Fact]
    public async Task PageCommandsMoveThroughThePages()
    {
        var harness = new MainViewModelHarness();
        var vm = harness.ViewModel;
        vm.PageSize = 2;
        for (var i = 1; i <= 5; i++)
        {
            await vm.Handle(Found($@"C:\Daten\f{i}.txt", 1), default);
        }

        Assert.False(vm.PreviousPageCommand.CanExecute(null));
        Assert.True(vm.NextPageCommand.CanExecute(null));

        vm.NextPageCommand.Execute(null);

        Assert.Equal(2, vm.CurrentPage);
        Assert.Equal([@"C:\Daten\f3.txt", @"C:\Daten\f4.txt"], FullPaths(vm));
        Assert.True(vm.PreviousPageCommand.CanExecute(null));

        vm.NextPageCommand.Execute(null);

        Assert.Equal(3, vm.CurrentPage);
        Assert.Equal([@"C:\Daten\f5.txt"], FullPaths(vm));
        Assert.False(vm.NextPageCommand.CanExecute(null));

        vm.PreviousPageCommand.Execute(null);

        Assert.Equal(2, vm.CurrentPage);
        Assert.Equal([@"C:\Daten\f3.txt", @"C:\Daten\f4.txt"], FullPaths(vm));
    }

    [Fact]
    public async Task APageBeyondTheLastIsClampedToTheLastPage()
    {
        var harness = new MainViewModelHarness();
        var vm = harness.ViewModel;
        vm.PageSize = 2;
        for (var i = 1; i <= 3; i++)
        {
            await vm.Handle(Found($@"C:\Daten\f{i}.txt", 1), default);
        }

        vm.CurrentPage = 9;

        Assert.Equal(2, vm.CurrentPage);
        Assert.Equal([@"C:\Daten\f3.txt"], FullPaths(vm));
    }

    [Fact]
    public async Task APageBelowOneIsRaisedToTheFirstPage()
    {
        var harness = new MainViewModelHarness();
        var vm = harness.ViewModel;
        await vm.Handle(Found(FileA, 1), default);

        vm.CurrentPage = 0;

        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal([FileA], FullPaths(vm));
    }

    [Fact]
    public async Task EnlargingThePageSizeOnALaterPageReturnsToPageOne()
    {
        var harness = new MainViewModelHarness();
        var vm = harness.ViewModel;
        vm.PageSize = 2;
        for (var i = 1; i <= 4; i++)
        {
            await vm.Handle(Found($@"C:\Daten\f{i}.txt", 1), default);
        }

        vm.CurrentPage = 2;
        vm.PageSize = 3;

        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(2, vm.TotalPages);
        Assert.Equal(3, vm.PagedLogTreeNodes.Count);
    }

    [Fact(Skip = "Suspected defect V11 (work package 5.7): shrinking PageSize on page 1 leaves the old, longer list displayed. Enable once decided.")]
    public async Task ShrinkingThePageSizeOnPageOneShortensTheDisplayedList()
    {
        var harness = new MainViewModelHarness();
        var vm = harness.ViewModel;
        for (var i = 1; i <= 5; i++)
        {
            await vm.Handle(Found($@"C:\Daten\f{i}.txt", 1), default);
        }

        vm.PageSize = 2;

        Assert.Equal(2, vm.PagedLogTreeNodes.Count);
        Assert.Equal(3, vm.TotalPages);
    }

    [Fact]
    public async Task ExpandingAFileNodeScansThatFileWithTheEnabledRules()
    {
        var harness = new MainViewModelHarness();
        var enabled = Rule("IBAN");
        await LoadRulesAsync(harness, enabled, Rule("Phone", enabled: false));
        await harness.ViewModel.Handle(Found(FileA, 1), default);
        var node = Assert.IsType<FileLogNodeViewModel>(Assert.Single(harness.ViewModel.PagedLogTreeNodes));
        ScanFileCommand? sent = null;
        harness.Mediator
            .Setup(m => m.Send(It.IsAny<ScanFileCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<ScanResultDto>, CancellationToken>((c, _) => sent = (ScanFileCommand)c)
            .Returns(() => new ValueTask<ScanResultDto>(
                new ScanResultDto { FilePath = new FileInfo(FileA), Warnings = [Warning()] }));

        node.IsExpanded = true;

        Assert.NotNull(sent);
        Assert.Equal(FileA, sent.FilePath.FullName);
        Assert.Equal(enabled.RuleId, Assert.Single(sent.RegexRuleList!).RuleId);
        Assert.False(sent.UseSpacy);
        Assert.True(node.IsLoaded);
        Assert.False(node.IsLoading);
    }

    [Fact]
    public async Task TheDetailsOfAFileAreGroupedByDetectionType()
    {
        var (harness, node) = await WithOneFileAsync();
        ReturnDetails(
            harness,
            Warning(ScanWarningType.Rule, "IBAN"),
            Warning(ScanWarningType.Rule, "Phone"),
            Warning(ScanWarningType.SpaCy, null, SpaCyLabel.Per));

        node.IsExpanded = true;

        Assert.Equal(2, node.Children.Count);
        var regex = node.Children.OfType<TypeLogNodeViewModel>().Single(t => t.Type == ScanWarningType.Rule);
        var spacy = node.Children.OfType<TypeLogNodeViewModel>().Single(t => t.Type == ScanWarningType.SpaCy);
        Assert.Equal("Regex/Rule (2)", regex.Title);
        Assert.Equal(2, regex.WarningCount);
        Assert.Equal("SpaCy (1)", spacy.Title);
        Assert.Equal(1, spacy.WarningCount);
        Assert.Equal("Lade Gruppen...", Assert.IsType<LoadingLogNodeViewModel>(Assert.Single(regex.Children)).Title);
        Assert.All(regex.Warnings, w => Assert.Equal(FileA, w.FilePath));
    }

    [Fact]
    public async Task ExpandingAnAlreadyLoadedFileNodeDoesNotScanAgain()
    {
        var (harness, node) = await WithOneFileAsync();
        ReturnDetails(harness, Warning());
        node.IsExpanded = true;

        node.IsExpanded = false;
        node.IsExpanded = true;

        harness.Mediator.Verify(m => m.Send(It.IsAny<ScanFileCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExpandingAFileNodeWhileItsDetailsLoadDoesNotStartASecondScan()
    {
        var (harness, node) = await WithOneFileAsync();
        var gate = new TaskCompletionSource<ScanResultDto>();
        harness.Mediator
            .Setup(m => m.Send(It.IsAny<ScanFileCommand>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<ScanResultDto>(gate.Task));

        node.IsExpanded = true;

        Assert.True(node.IsLoading);

        node.IsExpanded = false;
        node.IsExpanded = true;
        gate.SetResult(new ScanResultDto { FilePath = new FileInfo(FileA), Warnings = [Warning()] });

        harness.Mediator.Verify(m => m.Send(It.IsAny<ScanFileCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(node.IsLoading);
        Assert.True(node.IsLoaded);
        Assert.IsType<TypeLogNodeViewModel>(Assert.Single(node.Children));
    }

    [Fact]
    public async Task AFailingDetailScanShowsAnErrorChildAndAllowsARetry()
    {
        var (harness, node) = await WithOneFileAsync();
        harness.Mediator
            .Setup(m => m.Send(It.IsAny<ScanFileCommand>(), It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException<ScanResultDto>(new IOException("gone")));

        node.IsExpanded = true;

        var child = Assert.IsType<LoadingLogNodeViewModel>(Assert.Single(node.Children));
        Assert.Equal("Fehler beim Laden", child.Title);
        Assert.False(node.IsLoaded);
        Assert.False(node.IsLoading);

        ReturnDetails(harness, Warning());
        node.IsExpanded = false;
        node.IsExpanded = true;

        Assert.True(node.IsLoaded);
        Assert.IsType<TypeLogNodeViewModel>(Assert.Single(node.Children));
    }

    [Fact]
    public async Task ExpandingATypeNodeListsItsWarningsGroupedByRuleName()
    {
        var (harness, node) = await WithOneFileAsync();
        ReturnDetails(
            harness,
            Warning(ScanWarningType.Rule, "IBAN"),
            Warning(ScanWarningType.Rule, "IBAN"),
            Warning(ScanWarningType.Rule, "Phone"),
            Warning(ScanWarningType.Rule, null));
        node.IsExpanded = true;
        var type = Assert.IsType<TypeLogNodeViewModel>(Assert.Single(node.Children));

        type.IsExpanded = true;

        Assert.True(type.IsLoaded);
        Assert.False(type.IsLoading);
        var groups = type.Children.Cast<GroupLogNodeViewModel>().ToList();
        Assert.Equal(["IBAN (2)", "Phone (1)", "Unknown (1)"], groups.Select(g => g.Title));
        Assert.Equal(["IBAN", "Phone", "Unknown"], groups.Select(g => g.GroupName));
        Assert.Equal([2, 1, 1], groups.Select(g => g.WarningCount));
        Assert.All(groups, g => Assert.Equal("Lade Einträge...", Assert.IsType<LoadingLogNodeViewModel>(Assert.Single(g.Children)).Title));
    }

    [Fact]
    public async Task ExpandingASpacyTypeNodeGroupsByLabel()
    {
        var (harness, node) = await WithOneFileAsync();
        ReturnDetails(
            harness,
            Warning(ScanWarningType.SpaCy, null, SpaCyLabel.Per),
            Warning(ScanWarningType.SpaCy, null, SpaCyLabel.Per),
            Warning(ScanWarningType.SpaCy, null, SpaCyLabel.Loc),
            Warning(ScanWarningType.SpaCy, null, null));
        node.IsExpanded = true;
        var type = Assert.IsType<TypeLogNodeViewModel>(Assert.Single(node.Children));

        type.IsExpanded = true;

        var groups = type.Children.Cast<GroupLogNodeViewModel>().ToList();
        Assert.Equal(["Per (2)", "Loc (1)", "Unknown (1)"], groups.Select(g => g.Title));
    }

    [Fact]
    public async Task ExpandingALoadedTypeNodeAgainKeepsItsGroups()
    {
        var (harness, node) = await WithOneFileAsync();
        ReturnDetails(harness, Warning(ScanWarningType.Rule, "IBAN"));
        node.IsExpanded = true;
        var type = Assert.IsType<TypeLogNodeViewModel>(Assert.Single(node.Children));
        type.IsExpanded = true;
        var group = Assert.Single(type.Children);

        type.IsExpanded = false;
        type.IsExpanded = true;

        Assert.Same(group, Assert.Single(type.Children));
    }

    [Fact]
    public async Task ExpandingAGroupNodeShowsItsEntries()
    {
        var (harness, node) = await WithOneFileAsync();
        ReturnDetails(harness, Warning(line: 4), Warning(line: 9));
        node.IsExpanded = true;
        var type = Assert.IsType<TypeLogNodeViewModel>(Assert.Single(node.Children));
        type.IsExpanded = true;
        var group = Assert.IsType<GroupLogNodeViewModel>(Assert.Single(type.Children));

        group.IsExpanded = true;

        Assert.True(group.IsLoaded);
        Assert.False(group.IsLoading);
        var entries = group.Children.Cast<EntryLogNodeViewModel>().ToList();
        Assert.Equal([4, 9], entries.Select(e => e.Entry.Line));
        Assert.All(entries, e => Assert.Equal(FileA, e.Entry.FilePath));
        Assert.All(entries, e => Assert.Equal("IBAN", e.Entry.RuleName));
    }

    [Fact]
    public async Task AGroupNodeShowsAtMostTenEntriesAndNamesTheRest()
    {
        var (harness, node) = await WithOneFileAsync();
        ReturnDetails(harness, Enumerable.Range(1, 13).Select(i => Warning(line: i)).ToArray());
        node.IsExpanded = true;
        var type = Assert.IsType<TypeLogNodeViewModel>(Assert.Single(node.Children));
        type.IsExpanded = true;
        var group = Assert.IsType<GroupLogNodeViewModel>(Assert.Single(type.Children));

        group.IsExpanded = true;

        Assert.Equal(11, group.Children.Count);
        Assert.Equal(10, group.Children.OfType<EntryLogNodeViewModel>().Count());
        var note = Assert.IsType<LoadingLogNodeViewModel>(group.Children[^1]);
        Assert.Equal("... und 3 weitere (begrenzt auf 10)", note.Title);
    }

    [Fact]
    public async Task AGroupNodeWithExactlyTenEntriesHasNoOverflowNote()
    {
        var (harness, node) = await WithOneFileAsync();
        ReturnDetails(harness, Enumerable.Range(1, 10).Select(i => Warning(line: i)).ToArray());
        node.IsExpanded = true;
        var type = Assert.IsType<TypeLogNodeViewModel>(Assert.Single(node.Children));
        type.IsExpanded = true;
        var group = Assert.IsType<GroupLogNodeViewModel>(Assert.Single(type.Children));

        group.IsExpanded = true;

        Assert.Equal(10, group.Children.Count);
        Assert.All(group.Children, c => Assert.IsType<EntryLogNodeViewModel>(c));
    }

    [Fact]
    public async Task ExpandingALoadedGroupNodeAgainKeepsItsEntries()
    {
        var (harness, node) = await WithOneFileAsync();
        ReturnDetails(harness, Warning());
        node.IsExpanded = true;
        var type = Assert.IsType<TypeLogNodeViewModel>(Assert.Single(node.Children));
        type.IsExpanded = true;
        var group = Assert.IsType<GroupLogNodeViewModel>(Assert.Single(type.Children));
        group.IsExpanded = true;
        var entry = Assert.Single(group.Children);

        group.IsExpanded = false;
        group.IsExpanded = true;

        Assert.Same(entry, Assert.Single(group.Children));
    }

    [Fact]
    public async Task TypeNodeExpansionIsHandedToTheDispatcher()
    {
        var (harness, node) = await WithOneFileAsync();
        ReturnDetails(harness, Warning());
        node.IsExpanded = true;
        var type = Assert.IsType<TypeLogNodeViewModel>(Assert.Single(node.Children));
        harness.Dispatcher.RunImmediately = false;

        type.IsExpanded = true;

        Assert.True(type.IsLoading);
        Assert.False(type.IsLoaded);

        harness.Dispatcher.RunPending();

        Assert.False(type.IsLoading);
        Assert.True(type.IsLoaded);
    }

    private static List<string> FullPaths(MainViewModel vm) =>
        vm.PagedLogTreeNodes.Cast<FileLogNodeViewModel>().Select(n => n.FullPath).ToList();
}
