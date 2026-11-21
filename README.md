# Desktop Utility

A Windows tray application written in C# for rotating desktop wallpapers and optionally monitoring system resources. Open the gallery from the tray icon, or right-click the icon for rotation, interval, and favorite controls without opening the main window.

The application icon was created with AI.

## Run

- **Development:** Open `DesktopUtility.sln` in Visual Studio 2022 with the **.NET desktop development** workload and .NET 8 targeting packs. Press F5. The app starts in the Windows notification area and may appear under the tray overflow arrow.

Select **Help** in Gallery, Settings, or System monitoring for instructions on that page.

## Wallpaper controls

- Choose a folder with JPG, JPEG, PNG, or BMP files. The gallery scans that folder only. Each thumbnail shows its original resolution and file size. Hover over the details for the exact byte count. Select **Refresh** after changing the folder contents.
- Check images to include them in selected-only rotation. **Select all images** and **Clear selection** affect the entire folder, across all gallery pages. Click a card to apply that image or designate it as the favorite.
- Apply an image to all displays or select an individual display. To apply multiple wallpapers together, expand **Assign images by monitor**, select images in the gallery and assign them to each monitor, then click **Apply assigned wallpapers**. Automatic rotation can replace those wallpapers at the next interval. Select a gallery image and choose **Set monitor favorite** in the matching row to save a separate favorite for that monitor. The monitor list and image cards show these favorites after restart.
- Choose a **Target monitor** and use **Show images** to filter its pool. Hold **Ctrl** and **left-click** each image card to select multiple images. For a range, click the first card, then hold **Shift** and **left-click** the last. Add or remove the selected cards with the pool buttons. **Apply random image** uses that monitor's pool immediately. With per-monitor rotation enabled, monitors with a pool draw randomly from it, while monitors without one use the general rotation selection.
- Rotation runs at intervals of 1–1440 minutes. Applying a wallpaper manually restarts the interval. Turning off rotation leaves the current wallpaper in place.
- The settings page offers English, Hungarian, German, and Japanese. You can adjust the accent, text and surface colors, as well as window and panel opacity.
  The German and Japanese translations were prepared with the help of an external tool.
- Automatic launch is opt-in and applies at Windows sign-in. If the application is moved, enable the setting from the new location to update its path. Closing the main window returns it to the tray. Choose **Open Desktop Utility** from the quick panel to return to the main window, or **Exit** to stop the app.

## System monitoring

Open **System monitoring** and turn on its main switch. CPU, RAM, disks, NVIDIA GPU, integrated GPU and network metrics have individual switches. Sampling runs every two seconds while the page is open and stops when hidden or disabled. Preferences are saved locally.

Use **View** to switch between cards and history charts. Charts show up to 60 samples from the current monitoring session. The CPU card shows utilization, the average current clock reported by Windows in GHz, and the hottest readable CPU sensor. The RAM card shows used and total memory and the hottest readable memory module. The cards identify missing readings and show a link to the official driver page when PawnIO is missing.

Open **Choose drives** to show all mounted fixed and removable drives or select individual drives by letter. Each selected drive has its own labeled chart with separate read and write lines. The card header shows the combined rate. A drive gets an orange warning at 25% free space or less, and a red warning at 15% or less. CPU and GPU warnings are orange from 95% load and red at 100%.

Network rates show the total and the individual receive and send rates in Mbit/s for every active adapter that reports traffic statistics. This includes multiple Ethernet adapters, Wi-Fi, and Bluetooth network adapters when Windows reports them as active. Loopback and tunnel interfaces are excluded. Virtual adapters can contribute to the totals.

NVIDIA readings come from the installed driver through NVML and include GPU load, temperature and VRAM usage when supported. Integrated graphics are identified using DXCore and show the busiest engine reported by Windows performance counters.

For game FPS, open **Window FPS**, use **Download PresentMon 2.6.0**, select the official x64 console executable and click **Refresh windows**. Pick an open window and turn on **Measure FPS** for its monitor. Multiple selected windows from the same process share one capture. Leaving the page or turning off system monitoring stops capture. Refresh the list after moving a game to another monitor.

## External components

- **LibreHardwareMonitorLib 0.9.6 (MPL 2.0)** is included for CPU and RAM temperatures. Readings depend on the hardware, a [PawnIO driver](https://pawnio.eu/) where required, and sensor permissions. Restart Desktop Utility after installing the driver. See the [exact source version](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/tree/v0.9.6) and [license notices](licenses/).
- **PresentMon Console 2.6.0 x64 (MIT)** is an optional external download. Desktop Utility verifies the SHA-256 checksum of the official console asset before starting it. Windows must grant the account permission to start an ETW session. [Console release](https://github.com/GameTechDev/PresentMon/releases/tag/v2.6.0) · [License](https://github.com/GameTechDev/PresentMon/blob/v2.6.0/LICENSE.txt)

## Code layout

| Folder | Responsibility |
|---|---|
| `Views`, `PresentationControls` | WPF windows and controls, custom chrome, dialogs, and thumbnail display. |
| `ViewModels` | Gallery, wallpaper, help and monitoring view models, with separate disk and FPS folders under monitoring. |
| `Models` | Persistent settings and defaults. |
| `DesktopUtility.Core/Wallpaper` | Wallpaper service contract, rotation, monitor models and errors. |
| `DesktopUtility.Core/Monitoring` | Measurement records, enums, reader contract, sampling, chart history and FPS parsing. |
| `Services` | WPF-side rotation state, catalog loading and settings persistence. |
| `Infrastructure` | Platform adapters, including separate monitoring readers, Windows performance counters, hardware interfaces and FPS capture. |
| `Localization`, `Themes` | Four interface languages and shared WPF styles and brushes. |

`App.xaml.cs` composes the application and manages its lifetime. `DesktopUtility.Core` is a plain .NET library. It accepts a `TimeProvider` and an `IWallpaperService` so rotation can be tested without WPF or Windows wallpaper changes. The WPF application supplies the timer and Windows adapter. The appearance sliders control the alpha of the background and content-panel brushes while text and controls stay readable. Launch with `--open` to show the main window immediately during development.

The application targets .NET 8 to match the development and target machines.

A new metric defines its reading in Core and implements `ISystemMetricReader`. `WindowsMetricReaders` supplies the platform reader, and the monitoring view models present its data. The sampler picks up individual `SystemMetric` flags, and `SampleHistory<T>` supplies bounded history without WPF dependencies.

## Tests

Run `dotnet test DesktopUtility.sln` or open Test Explorer in Visual Studio. Core tests cover wallpaper rotation, monitoring samples, alert thresholds and FPS data parsing. The Windows WPF tests cover settings persistence, tray navigation, separate drive charts and shared FPS capture.

## Roadmap

1. **Move to .NET 10.** Update the target framework when development moves to Visual Studio 2026 and verify the application on the target Windows versions.
