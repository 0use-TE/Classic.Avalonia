# Classic Demo

The shared sample follows the Crystal.Avalonia example application model,
using Crystal.Avalonia 3.1.1 and CommunityToolkit.Mvvm 8.4.0.
The Classic control libraries themselves do not depend on Crystal.

- `App` derives from `CrystalApplication`, registers the main View/ViewModel
  pair with `AddMvvmTransient`, registers `DemoModule`, and creates the platform
  shell with `CreateShell<MainWindow, MainView>()`.
- `DemoModule` registers the data-display model. Crystal builds the container
  before creating views, so constructor dependencies are available during
  automatic ViewModel wiring.
- `Views` contains the desktop window and shared main view. The main view uses
  `ViewModelLocator.AutoWireViewModel` rather than constructing its own model.
- `ViewModels` contains the observable base model, main model, and data models.
  Category selection is shared through a two-way observable property.
  Each main view receives independent models and editable sample data.
- The Buttons category uses Crystal `EventToCommand` with explicit routed events
  for its click counter and spinner. `PassEventArgs` forwards spin direction to
  the model. Control-specific layout and owned dialogs remain view responsibilities.
- View code handles layout, control-specific input and owned dialogs. The
  existing control gallery remains in `MainView.axaml`.

Desktop, Browser, Android and iOS hosts share this application. Only the desktop
host creates a window; Browser/iOS use a main view and Android uses the activity
view factory. All hosts register the embedded Tahoma regular/bold UI family and
Liberation Sans fallback through `WithClassicFont`.

## Sounds

The desktop Demo, Notepad and Explorer opt into the shared message-box sound
helper. Browser, Android and iOS do not currently implement sample audio.

| Trigger | Audio |
| --- | --- |
| Error or Warning message box loaded | chord.wav |
| Information or Question message box loaded | ding.wav |
| Message box without an icon | None |
| Buttons, menus, date/time changes, window startup/shutdown | None |

`chimes.wav` is packaged as a sample asset but has no trigger. These are the
sample's current mappings, not a complete implementation of the Windows 98
system sound scheme. OS playback success depends on the local audio device;
the automated tests verify the assets, not audible output.

Run the desktop sample:

```powershell
dotnet run --project samples/Classic.Demo.Desktop
```

Run the browser sample and open the HTTP URL printed by its host:

```powershell
dotnet run --project samples/Classic.Demo.Browser
```

See `tests/README.md` for the verification matrix and remaining platform limits.
