using ITTitans.PrivacyScanner.Model;
using ITTitans.PrivacyScanner.UI.ViewModels;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.ViewModels;

/// <summary>
/// Covers the tree node types of the results view and their lazy-load trigger.
/// </summary>
public class LogTreeNodeViewModelTests
{
    [Fact]
    public void BaseProperties_StoreValuesAndNotify()
    {
        var node = new LoadingLogNodeViewModel();
        var changed = new List<string?>();
        node.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        node.Title = "t";
        node.WarningCount = 3;
        node.IsLoaded = true;
        node.IsLoading = true;

        Assert.Equal("t", node.Title);
        Assert.Equal(3, node.WarningCount);
        Assert.True(node.IsLoaded);
        Assert.True(node.IsLoading);
        Assert.Equal(new[] { "Title", "WarningCount", "IsLoaded", "IsLoading" }, changed);
        Assert.Empty(node.Children);
    }

    [Fact]
    public void LoadingNode_ExpandingOnlySetsTheFlag()
    {
        var node = new LoadingLogNodeViewModel { IsExpanded = true };

        Assert.True(node.IsExpanded);
    }

    [Fact]
    public void EntryNode_HoldsItsEntry()
    {
        var entry = new LogEntryViewModel { Line = 4 };
        var node = new EntryLogNodeViewModel { Entry = entry };

        Assert.Same(entry, node.Entry);
    }

    [Fact]
    public void FileNode_ExpandingRaisesExpandedOnce()
    {
        var node = new FileLogNodeViewModel { FullPath = @"C:\x.txt" };
        var raised = new List<FileLogNodeViewModel>();
        node.Expanded += raised.Add;

        node.IsExpanded = true;
        node.IsExpanded = true;

        Assert.Equal(@"C:\x.txt", node.FullPath);
        Assert.Same(node, Assert.Single(raised));
    }

    [Fact]
    public void FileNode_ExpandingAgainAfterCollapseRaisesAgainWhenNotLoaded()
    {
        var node = new FileLogNodeViewModel();
        var count = 0;
        node.Expanded += _ => count++;

        node.IsExpanded = true;
        node.IsExpanded = false;
        node.IsExpanded = true;

        Assert.Equal(2, count);
    }

    [Fact]
    public void FileNode_CollapsingDoesNotRaise()
    {
        var node = new FileLogNodeViewModel();
        var count = 0;
        node.Expanded += _ => count++;

        node.IsExpanded = false;

        Assert.Equal(0, count);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void FileNode_LoadedOrLoading_SuppressesExpandedEvent(bool loaded, bool loading)
    {
        var node = new FileLogNodeViewModel { IsLoaded = loaded, IsLoading = loading };
        var count = 0;
        node.Expanded += _ => count++;

        node.IsExpanded = true;

        Assert.Equal(0, count);
    }

    [Fact]
    public void FileNode_ExpandingWithoutSubscriberIsHarmless()
    {
        var node = new FileLogNodeViewModel();

        node.IsExpanded = true;

        Assert.True(node.IsExpanded);
    }

    [Fact]
    public void TypeNode_RaisesExpandedAndKeepsItsData()
    {
        var warnings = new List<LogEntryViewModel> { new() };
        var node = new TypeLogNodeViewModel { Type = ScanWarningType.SpaCy, Warnings = warnings };
        TypeLogNodeViewModel? raised = null;
        node.Expanded += n => raised = n;

        node.IsExpanded = true;

        Assert.Same(node, raised);
        Assert.Equal(ScanWarningType.SpaCy, node.Type);
        Assert.Same(warnings, node.Warnings);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void TypeNode_LoadedOrLoading_SuppressesExpandedEvent(bool loaded, bool loading)
    {
        var node = new TypeLogNodeViewModel { IsLoaded = loaded, IsLoading = loading };
        var count = 0;
        node.Expanded += _ => count++;

        node.IsExpanded = true;

        Assert.Equal(0, count);
    }

    [Fact]
    public void TypeNode_DefaultsAreEmptyAndExpandingWithoutSubscriberIsHarmless()
    {
        var node = new TypeLogNodeViewModel();

        Assert.Empty(node.Warnings);
        node.IsExpanded = true;
        Assert.True(node.IsExpanded);
    }

    [Fact]
    public void GroupNode_RaisesExpandedAndKeepsItsData()
    {
        var warnings = new List<LogEntryViewModel> { new(), new() };
        var node = new GroupLogNodeViewModel { GroupName = "Email", Warnings = warnings };
        GroupLogNodeViewModel? raised = null;
        node.Expanded += n => raised = n;

        node.IsExpanded = true;

        Assert.Same(node, raised);
        Assert.Equal("Email", node.GroupName);
        Assert.Equal(2, node.Warnings.Count);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void GroupNode_LoadedOrLoading_SuppressesExpandedEvent(bool loaded, bool loading)
    {
        var node = new GroupLogNodeViewModel { IsLoaded = loaded, IsLoading = loading };
        var count = 0;
        node.Expanded += _ => count++;

        node.IsExpanded = true;

        Assert.Equal(0, count);
    }

    [Fact]
    public void GroupNode_DefaultsAreEmptyAndExpandingWithoutSubscriberIsHarmless()
    {
        var node = new GroupLogNodeViewModel();

        Assert.Equal(string.Empty, node.GroupName);
        Assert.Empty(node.Warnings);
        node.IsExpanded = true;
        Assert.True(node.IsExpanded);
    }
}
