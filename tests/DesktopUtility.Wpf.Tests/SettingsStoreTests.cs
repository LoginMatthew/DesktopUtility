using System.IO;
using DesktopUtility.Core.Monitoring;
using DesktopUtility.Infrastructure.Persistence;
using DesktopUtility.Models;
using Xunit;

namespace DesktopUtility.Wpf.Tests;

public sealed class SettingsStoreTests
{
    [Fact]
    public void OlderSettingsKeepTheirWallpaperAssignments()
    {
        WithTemporarySettings((store, path) =>
        {
            File.WriteAllText(path, """
                {
                  "Language": "hu",
                  "Favorite": "global.jpg",
                  "MonitorAssignments": { "LEFT": "assigned.jpg" }
                }
                """);

            var settings = store.Load();

            Assert.Equal("hu", settings.Language);
            Assert.Equal("global.jpg", settings.Favorite);
            Assert.Equal("assigned.jpg", settings.MonitorAssignments["left"]);
            Assert.Empty(settings.MonitorFavorites);
        });
    }

    [Fact]
    public void MonitorFavoritesSurviveReloadSeparatelyFromAssignmentsAndTheGlobalFavorite()
    {
        WithTemporarySettings((store, path) =>
        {
            var settings = new Settings
            {
                Favorite = "global.jpg",
                MonitorAssignments = { ["LEFT"] = "assigned.jpg" },
                MonitorFavorites = { ["LEFT"] = "first.jpg", ["RIGHT"] = "second.jpg" }
            };

            store.Save(settings);

            var restored = new SettingsStore(path).Load();

            Assert.Equal("global.jpg", restored.Favorite);
            Assert.Equal("assigned.jpg", restored.MonitorAssignments["left"]);
            Assert.Equal("first.jpg", restored.MonitorFavorites["left"]);
            Assert.Equal("second.jpg", restored.MonitorFavorites["right"]);
        });
    }

    [Fact]
    public void MonitorPoolsPersistIndependentlyAndUseCaseInsensitiveLookups()
    {
        WithTemporarySettings((store, path) =>
        {
            File.WriteAllText(path, """
                {
                  "MonitorPools": {
                    "LEFT": ["first.jpg", "FIRST.jpg", "second.jpg", ""],
                    "RIGHT": ["third.jpg"],
                    "UNUSED": null
                  },
                  "MonitorFavorites": { "LEFT": "favorite.jpg" }
                }
                """);

            var settings = store.Load();
            store.Save(settings);
            var restored = new SettingsStore(path).Load();

            Assert.Equal(2, restored.MonitorPools.Count);
            Assert.Equal(2, restored.MonitorPools["left"].Count);
            Assert.Contains("SECOND.jpg", restored.MonitorPools["left"]);
            Assert.Equal("third.jpg", Assert.Single(restored.MonitorPools["right"]));
            Assert.Equal("favorite.jpg", restored.MonitorFavorites["left"]);
        });
    }

    [Fact]
    public void MonitoringStartsDisabledAndRetainsTheSelectedMetricsAfterReload()
    {
        WithTemporarySettings((store, path) =>
        {
            File.WriteAllText(path, """{"Language":"hu"}""");
            var settings = store.Load();
            Assert.False(settings.MonitoringEnabled);
            Assert.Equal(SystemMetric.All, settings.MonitoringMetrics);
            Assert.True(settings.MonitorAllDisks);

            settings.MonitoringEnabled = true;
            settings.MonitoringMetrics = SystemMetric.Cpu | SystemMetric.IntegratedGpu | SystemMetric.Network;
            settings.ShowMonitoringCharts = true;
            settings.PresentMonPath = @"C:\Tools\PresentMon.exe";
            settings.MonitorAllDisks = false;
            settings.MonitoredDriveRoots.Add("C:\\");
            store.Save(settings);
            var restored = new SettingsStore(path).Load();

            Assert.True(restored.MonitoringEnabled);
            Assert.Equal(SystemMetric.Cpu | SystemMetric.IntegratedGpu | SystemMetric.Network, restored.MonitoringMetrics);
            Assert.True(restored.ShowMonitoringCharts);
            Assert.Equal(@"C:\Tools\PresentMon.exe", restored.PresentMonPath);
            Assert.False(restored.MonitorAllDisks);
            Assert.Contains("c:\\", restored.MonitoredDriveRoots);
        });
    }

    private static void WithTemporarySettings(Action<SettingsStore, string> test)
    {
        string path = Path.Combine(Path.GetTempPath(), $"DesktopUtility-settings-{Guid.NewGuid():N}.json");
        try
        {
            test(new SettingsStore(path), path);
        }
        finally
        {
            File.Delete(path);
            File.Delete(path + ".tmp");
        }
    }
}
