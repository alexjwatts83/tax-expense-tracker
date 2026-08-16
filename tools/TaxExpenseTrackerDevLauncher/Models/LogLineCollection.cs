using System.Collections.ObjectModel;

namespace TaxExpenseTrackerDevLauncher.Models;

public sealed class LogLineCollection : ObservableCollection<LogLine>
{
    private readonly int _maximumCount;

    public LogLineCollection(int maximumCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCount);
        _maximumCount = maximumCount;
    }

    protected override void InsertItem(int index, LogLine item)
    {
        while (Count >= _maximumCount)
            RemoveAt(0);

        base.InsertItem(Count, item);
    }
}