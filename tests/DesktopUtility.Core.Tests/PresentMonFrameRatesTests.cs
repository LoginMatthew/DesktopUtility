using DesktopUtility.Core.Monitoring;
using Xunit;

namespace DesktopUtility.Core.Tests;

public sealed class PresentMonFrameRatesTests
{
    [Fact]
    public void TakeFramesPerSecondUsesTheMostActiveDisplayedStreamForTheSelectedProcess()
    {
        var frames = new PresentMonFrameRates(42);
        frames.AddLine("Application,ProcessID,SwapChainAddress,DisplayedTime");
        frames.AddLine("\"Game, Main.exe\",42,0x10,15");
        frames.AddLine("Game.exe,42,0x10,25");
        frames.AddLine("Game.exe,42,0x10,NA");
        frames.AddLine("Game.exe,42,0x11,100");
        frames.AddLine("Game.exe,43,0x10,100");
        frames.AddLine("Game.exe,42,0x10,Infinity");

        Assert.Equal(50d, frames.TakeFramesPerSecond());
        Assert.Null(frames.TakeFramesPerSecond());
    }

    [Fact]
    public void TakeFramesPerSecondSupportsLegacyDisplayTimingAndIgnoresDroppedFrames()
    {
        var frames = new PresentMonFrameRates(17);
        frames.AddLine("ProcessID,SwapChainAddress,Dropped,MsBetweenDisplayChange");
        frames.AddLine("17,0x2,1,10");
        frames.AddLine("17,0x2,0,0");
        Assert.Null(frames.TakeFramesPerSecond());

        frames.AddLine("17,0x2,0,40");
        Assert.Equal(25d, frames.TakeFramesPerSecond());
    }
}
