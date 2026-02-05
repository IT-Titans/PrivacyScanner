using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace ITTitans.PrivacyScanner.UI.Models;

public class BulkObservableCollection<T> : ObservableCollection<T>
{
    public void AddRange(IEnumerable<T> items, int? maxCount = null)
    {
        if (items == null) return;

        CheckReentrancy();

        var addedAny = false;
        foreach (var item in items)
        {
            Items.Add(item);
            addedAny = true;
        }

        if (maxCount.HasValue)
        {
            while (Items.Count > maxCount.Value)
            {
                Items.RemoveAt(0);
                addedAny = true;
            }
        }

        if (addedAny)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
