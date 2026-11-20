using System.Diagnostics;
using System.IO;
using DesktopUtility.Infrastructure.Images;

namespace DesktopUtility.Services;

internal sealed class FolderLoader(WallpaperSession state, Action folderChanged) : IDisposable
{
    private CancellationTokenSource? catalogLoad;

    public async Task LoadAsync(string folder)
    {
        catalogLoad?.Cancel();
        var request = new CancellationTokenSource();
        catalogLoad = request;

        if (!string.IsNullOrEmpty(folder))
        {
            state.MessageKey = "Loading";
        }

        state.NotifyState();
        try
        {
            var images = await ImageCatalog.ReadAsync(folder, request.Token);
            request.Token.ThrowIfCancellationRequested();

            if (state.MessageKey == "Loading")
            {
                state.MessageKey = null;
            }

            state.SetFolderImages(folder, images);
            folderChanged();
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested)
        {
            // Cancellation is expected when a newer folder load starts or the app exits.
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {

            if (!request.IsCancellationRequested)
            {
                state.MessageKey = "FileError";
                state.NotifyState();
                Trace.TraceWarning(error.ToString());
            }
        }
        finally
        {

            if (ReferenceEquals(catalogLoad, request))
            {
                catalogLoad = null;
            }

            request.Dispose();
        }
    }

    public void Dispose() => catalogLoad?.Cancel();
}
