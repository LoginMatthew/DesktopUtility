using DesktopUtility.Core;
using Xunit;

namespace DesktopUtility.Core.Tests;

public sealed class WallpaperRotationCoordinatorTests
{
    private readonly ManualClock clock = new();
    private readonly RecordingWallpaperService wallpaper = new();
    private readonly WallpaperRotationCoordinator coordinator;

    public WallpaperRotationCoordinatorTests()
    {
        coordinator = new WallpaperRotationCoordinator(wallpaper, clock);
    }

    [Fact]
    public async Task ChangingIntervalRestartsRotation()
    {
        Configure(interval: 5);
        clock.Advance(TimeSpan.FromMinutes(4));
        Configure(interval: 10);
        clock.Advance(TimeSpan.FromMinutes(9));

        Assert.Equal(WallpaperActionResult.Skipped, await coordinator.TickAsync());
        Assert.Empty(wallpaper.Applied);

        clock.Advance(TimeSpan.FromMinutes(1));

        Assert.Equal(WallpaperActionResult.Applied, await coordinator.TickAsync());
        Assert.Equal(("one.jpg", null), Assert.Single(wallpaper.Applied));
    }

    [Fact]
    public async Task PausedRotationAllowsManualWallpaperChanges()
    {
        Configure(enabled: false);
        clock.Advance(TimeSpan.FromHours(1));

        Assert.Equal(WallpaperActionResult.Skipped, await coordinator.TickAsync());
        Assert.Equal(WallpaperActionResult.Applied, await coordinator.ApplyAsync("favorite.jpg", "right"));
        Assert.Equal(("favorite.jpg", "right"), Assert.Single(wallpaper.Applied));
        Assert.Null(coordinator.Remaining);
    }

    [Fact]
    public async Task SelectedImagesAreAssignedToDifferentMonitors()
    {
        Configure(separateMonitors: true, selected: new HashSet<string> { "two.jpg", "three.jpg" });
        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(WallpaperActionResult.Applied, await coordinator.TickAsync());
        Assert.Collection(wallpaper.Applied,
            item => Assert.Equal(("two.jpg", "left"), item),
            item => Assert.Equal(("three.jpg", "right"), item));
    }

    [Fact]
    public async Task EmptySelectionKeepsRotationIdle()
    {
        Configure(selected: new HashSet<string>());
        clock.Advance(TimeSpan.FromHours(1));

        Assert.Equal(WallpaperActionResult.Skipped, await coordinator.TickAsync());
        Assert.Empty(wallpaper.Applied);
        Assert.Null(coordinator.Remaining);
    }

    [Fact]
    public async Task MonitorFailureAllowsTheOtherMonitorToUpdate()
    {
        wallpaper.FailingMonitor = "left";
        Configure(separateMonitors: true);
        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(WallpaperActionResult.Failed, await coordinator.TickAsync());
        Assert.Equal(("two.jpg", "right"), Assert.Single(wallpaper.Applied));
    }

    [Fact]
    public async Task ManualAssignmentsKeepEachImageOnItsChosenMonitor()
    {
        Configure(enabled: false);

        var result = await coordinator.ApplyAssignmentsAsync([new("left", "three.jpg"), new("right", "one.jpg")]);

        Assert.Equal(WallpaperActionResult.Applied, result);
        Assert.Collection(wallpaper.Applied,
            item => Assert.Equal(("three.jpg", "left"), item),
            item => Assert.Equal(("one.jpg", "right"), item));
        Assert.Null(coordinator.Remaining);
    }

    [Fact]
    public async Task EmptyManualAssignmentsDoNotResetTheSchedule()
    {
        Configure();
        clock.Advance(TimeSpan.FromMinutes(2));

        Assert.Equal(WallpaperActionResult.Skipped, await coordinator.ApplyAssignmentsAsync([]));
        Assert.Empty(wallpaper.Applied);
        Assert.Equal(TimeSpan.FromMinutes(3), coordinator.Remaining);
    }

    private void Configure(int interval = 5, bool enabled = true, bool separateMonitors = false, IReadOnlySet<string>? selected = null)
    {
        coordinator.Configure(["one.jpg", "two.jpg", "three.jpg"], selected ?? new HashSet<string>(),
            selectedOnly: selected is not null, enabled, interval, separateMonitors);
    }

    private sealed class ManualClock : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => timestamp;
        public void Advance(TimeSpan elapsed) => timestamp += elapsed.Ticks;
    }

    private sealed class RecordingWallpaperService : IWallpaperService
    {
        public List<(string Path, string? MonitorId)> Applied { get; } = [];
        public string? FailingMonitor { get; set; }

        public Task<IReadOnlyList<MonitorTarget>> GetMonitorsAsync() =>
            Task.FromResult<IReadOnlyList<MonitorTarget>>([new("left", "Left"), new("right", "Right")]);

        public Task ApplyAsync(string filePath, string? monitorId)
        {

            if (monitorId is not null && monitorId == FailingMonitor)
            {
                throw new WallpaperOperationException(new IOException("Simulated monitor failure."));
            }

            Applied.Add((filePath, monitorId));

            return Task.CompletedTask;
        }
    }
}
