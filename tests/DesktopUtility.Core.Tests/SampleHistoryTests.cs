using DesktopUtility.Core.Monitoring;
using Xunit;

namespace DesktopUtility.Core.Tests;

public sealed class SampleHistoryTests
{
    [Fact]
    public void KeepsTheNewestSamplesInOrderIncludingGaps()
    {
        var history = new SampleHistory<double?>(3);
        history.Add(1);
        history.Add(2);
        history.Add(null);
        history.Add(4);

        Assert.Equal([2d, null, 4d], history.ToArray());

        history.Clear();
        Assert.Empty(history);
    }
}
