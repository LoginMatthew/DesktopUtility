namespace DesktopUtility.Infrastructure.Monitoring.Fps;

internal sealed class FpsCapturePool(Func<string, int, Action<double?>, CancellationToken, Task> startCapture)
{
    private readonly object gate = new();
    private readonly Dictionary<int, Capture> captures = new();

    public async Task SubscribeAsync(string executable, int processId, Action<double?> publish, CancellationToken cancellationToken)
    {
        Capture capture;
        long subscription;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (gate)
            {

                if (!captures.TryGetValue(processId, out capture!))
                {
                    capture = new Capture();
                    captures.Add(processId, capture);
                    var started = capture;
                    started.Work = Task.Run(() => startCapture(executable, processId,
                        value => Publish(started, value), started.Stop.Token));
                }

                if (!capture.Stopping)
                {
                    subscription = ++capture.NextSubscription;
                    capture.Subscribers.Add(subscription, publish);
                    break;
                }
            }

            await capture.Released.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await capture.Work.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await ReleaseAsync(processId, capture, subscription).ConfigureAwait(false);
        }
    }

    private void Publish(Capture capture, double? value)
    {
        Action<double?>[] recipients;
        lock (gate)
        {
            recipients = capture.Subscribers.Values.ToArray();
        }

        foreach (var recipient in recipients)
        {
            recipient(value);
        }
    }

    private async Task ReleaseAsync(int processId, Capture capture, long subscription)
    {
        lock (gate)
        {
            capture.Subscribers.Remove(subscription);

            if (capture.Subscribers.Count > 0)
            {

                return;
            }

            capture.Stopping = true;
        }

        try
        {
            capture.Stop.Cancel();
            await capture.Work.ConfigureAwait(false);
        }
        catch (Exception) when (capture.Work.IsCanceled || capture.Work.IsFaulted)
        {
            // Active subscribers receive the result through their own wait.
        }
        finally
        {
            lock (gate)
            {
                captures.Remove(processId);
            }

            capture.Stop.Dispose();
            capture.Released.TrySetResult();
        }
    }

    private sealed class Capture
    {
        public readonly CancellationTokenSource Stop = new();
        public readonly TaskCompletionSource Released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly Dictionary<long, Action<double?>> Subscribers = new();
        public Task Work { get; set; } = Task.CompletedTask;
        public long NextSubscription { get; set; }
        public bool Stopping { get; set; }
    }
}
