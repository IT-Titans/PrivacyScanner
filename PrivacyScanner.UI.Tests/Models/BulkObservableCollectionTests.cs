using System.Collections.Specialized;

using ITTitans.PrivacyScanner.UI.Models;

using Xunit;

namespace ITTitans.PrivacyScanner.UI.Tests.Models;

/// <summary>
/// Covers the batch-add collection that keeps the results list from raising one event per item.
/// </summary>
public class BulkObservableCollectionTests
{
    private static List<NotifyCollectionChangedAction> Track(BulkObservableCollection<int> c)
    {
        var actions = new List<NotifyCollectionChangedAction>();
        c.CollectionChanged += (_, e) => actions.Add(e.Action);
        return actions;
    }

    [Fact]
    public void AddRange_AddsAllItemsInOrderWithASingleReset()
    {
        var c = new BulkObservableCollection<int> { 1 };
        var actions = Track(c);

        c.AddRange(new[] { 2, 3, 4 });

        Assert.Equal(new[] { 1, 2, 3, 4 }, c);
        Assert.Equal(new[] { NotifyCollectionChangedAction.Reset }, actions);
    }

    [Fact]
    public void AddRange_Null_ChangesNothing()
    {
        var c = new BulkObservableCollection<int> { 1 };
        var actions = Track(c);

        c.AddRange(null!);

        Assert.Single(c);
        Assert.Empty(actions);
    }

    [Fact]
    public void AddRange_EmptySequence_RaisesNothing()
    {
        var c = new BulkObservableCollection<int>();
        var actions = Track(c);

        c.AddRange(Array.Empty<int>());

        Assert.Empty(c);
        Assert.Empty(actions);
    }

    [Fact]
    public void AddRange_WithMaxCount_DropsTheOldestItems()
    {
        var c = new BulkObservableCollection<int> { 1, 2 };
        var actions = Track(c);

        c.AddRange(new[] { 3, 4, 5 }, maxCount: 3);

        Assert.Equal(new[] { 3, 4, 5 }, c);
        Assert.Single(actions);
    }

    [Fact]
    public void AddRange_MaxCountNotExceeded_KeepsEverything()
    {
        var c = new BulkObservableCollection<int>();

        c.AddRange(new[] { 1, 2 }, maxCount: 10);

        Assert.Equal(new[] { 1, 2 }, c);
    }

    [Fact]
    public void AddRange_EmptyButOverLimit_TrimsAndRaisesReset()
    {
        var c = new BulkObservableCollection<int> { 1, 2, 3 };
        var actions = Track(c);

        c.AddRange(Array.Empty<int>(), maxCount: 2);

        Assert.Equal(new[] { 2, 3 }, c);
        Assert.Single(actions);
    }
}
