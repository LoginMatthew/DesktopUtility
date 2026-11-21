using DesktopUtility.Infrastructure.Monitoring.Fps;
using Xunit;

namespace DesktopUtility.Wpf.Tests;

public sealed class FpsCapturePoolTests
{
    [Fact]
    public async Task TwoSubscribersShareOneCaptureUntilTheLastSubscriberStops()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int starts = 0;
        var pool = new FpsCapturePool(async (_, _, _, token) =>
        {
            Interlocked.Increment(ref starts);
            started.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
            finally
            {
                stopped.TrySetResult();
            }
        });
        using var first = new CancellationTokenSource();
        using var second = new CancellationTokenSource();
        var firstRow = pool.SubscribeAsync("PresentMon.exe", 17, _ => { }, first.Token);
        var secondRow = pool.SubscribeAsync("PresentMon.exe", 17, _ => { }, second.Token);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);

        first.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstRow);
        Assert.False(stopped.Task.IsCompleted);
        Assert.Equal(1, Volatile.Read(ref starts));

        second.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => secondRow);
        Assert.True(stopped.Task.IsCompletedSuccessfully);
        Assert.Equal(1, Volatile.Read(ref starts));
    }
}
