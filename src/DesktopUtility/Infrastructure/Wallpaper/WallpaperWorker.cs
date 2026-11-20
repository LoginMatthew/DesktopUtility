using System.Windows.Threading;

namespace DesktopUtility.Infrastructure.Wallpaper;

internal sealed class WallpaperWorker : IDisposable
{
    private readonly TaskCompletionSource<Dispatcher> ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public WallpaperWorker()
    {
        var thread = new Thread(() =>
        {
            ready.SetResult(Dispatcher.CurrentDispatcher);
            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "Desktop Utility wallpaper"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    public async Task<T> RunAsync<T>(Func<T> action)
    {
        var dispatcher = await ready.Task.ConfigureAwait(false);

        return await dispatcher.InvokeAsync(action).Task.ConfigureAwait(false);
    }

    public void Dispose()
    {

        if (ready.Task.IsCompletedSuccessfully)
        {
            ready.Task.Result.BeginInvokeShutdown(DispatcherPriority.Send);
        }
    }
}
