# Desktop Utility

A Windows tray application for rotating desktop wallpapers. Open the gallery from the tray icon, or right-click the icon for rotation, interval, and favorite controls without opening the main window.

The application icon was created with AI.

## Run

- **Development:** Open `DesktopUtility.sln` in Visual Studio 2022 with the **.NET desktop development** workload and .NET 8 targeting packs. Press F5. The app starts in the Windows notification area and may appear under the tray overflow arrow.

## Wallpaper controls

- Choose a folder with JPG, JPEG, PNG, or BMP files. The gallery scans that folder only. Each thumbnail shows its original resolution and file size. Hover over the details for the exact byte count. Select **Refresh** after changing the folder contents.
- Check images to include them in selected-only rotation. Click a card to apply that image or designate it as the favorite.
- Apply an image to all displays or select an individual display. Automatic rotation can use the same image for every display or assign different images when enough candidates exist.
- Rotation runs at intervals of 1–1440 minutes. Applying a wallpaper manually restarts the interval. Turning off rotation leaves the current wallpaper in place.
- The settings page offers English, Hungarian, German, and Japanese. You can adjust the accent, text and surface colors, as well as window and panel opacity.
- Automatic launch is opt-in and applies at Windows sign-in. If the application is moved, enable the setting from the new location to update its path. Closing the main window returns it to the tray. Choose **Open Desktop Utility** from the quick panel to return to the main window, or **Exit** to stop the app.

## Code layout

| Folder | Responsibility |
|---|---|
| `Views`, `PresentationControls` | WPF windows and controls, custom chrome, dialogs, and thumbnail display. |
| `ViewModels` | Gallery operations, settings bindings, rotation controls, and display state. |
| `Models` | Persistent settings and defaults. |
| `DesktopUtility.Core` | Wallpaper scheduling, candidate selection, monitor assignment and the wallpaper service contract. |
| `Services` | WPF-side rotation state, catalog loading and settings persistence. |
| `Infrastructure` | Platform adapters grouped into Images, Persistence, Startup, and Wallpaper. |
| `Localization`, `Themes` | Four interface languages and shared WPF styles and brushes. |

`App.xaml.cs` composes the application and manages its lifetime. `DesktopUtility.Core` is a plain .NET library. It accepts a `TimeProvider` and an `IWallpaperService` so rotation can be tested without WPF or Windows wallpaper changes. The WPF application supplies the timer and Windows adapter. The appearance sliders control the alpha of the background and content-panel brushes while text and controls stay readable. Launch with `--open` to show the main window immediately during development.

The current solution uses only framework libraries and targets .NET 8 to match the development and target machines.

## Roadmap

1. **Add automated tests.** A separate test project will cover interval changes, paused rotation, selected images and multi-monitor assignment. Additional tests will cover settings persistence and launching the main window from the tray.
2. **Add optional system monitoring.** Display CPU load, RAM usage, disk activity and NVIDIA GPU utilization. A master switch and per-metric switches will control collection. Turning a metric off will stop its sampling.
3. **Move to .NET 10.** Update the target framework when development moves to Visual Studio 2026 and verify the application on the target Windows versions.
