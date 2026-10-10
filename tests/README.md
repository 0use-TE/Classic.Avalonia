# Verification

Verified on Windows with .NET SDK 10.0.401 on 2026-10-10. The current Release suite passes 63 test cases, with no skipped cases. These results establish functional smoke coverage, not exhaustive platform certification. Following the user's instruction, the theme updates use build-only checks for Browser and mobile hosts.

## Commands

```powershell
dotnet build Classic.Avalonia.sln -c Release
dotnet test tests/Classic.Avalonia.Tests/Classic.Avalonia.Tests.csproj -c Release --no-build
dotnet build samples/Classic.Demo.Android/Classic.Demo.Android.csproj -c Release
dotnet build samples/Classic.Demo.iOS/Classic.Demo.iOS.csproj -c Debug -r iossimulator-arm64 -p:EnableCodeSigning=false
dotnet build samples/Classic.Demo.Browser/Classic.Demo.Browser.csproj -c Release
```

Use the HTTP URL printed by the Browser host. Restart that host after rebuilding: static asset fingerprints can change. Android needs its SDK/JDK and .NET workload; complete iOS app packaging and execution need macOS/Xcode. The iOS command on Windows only verified managed compilation.

## Project Coverage

| Project | Verification |
| --- | --- |
| Classic.CommonControls.Avalonia | Release build/package; list selection with mouse/keyboard, toolbars, dialogs, platform independence |
| Classic.Avalonia.Theme | Release build/package; control templates, window lifecycle, font faces/glyphs, native and overlay picker paths |
| Classic.Avalonia.Theme.ColorPicker | Release build/package; RGB editor, spectrum slider/color synchronization and custom-color visibility toggle |
| Classic.Demo | Release build; Crystal application/module initialization, automatic and model-first view binding, transient model isolation, category binding in both directions, all categories, responsive layout, inline dialogs, date/time pointer input inside scroll containers |
| Classic.Demo.Desktop | Release/Debug builds; MainWindow instantiated in Headless tests; native desktop screenshots reviewed for overview, menus and navigation |
| Classic.Demo.Browser | Release WebAssembly build; no runtime/UI checks for the Tahoma update |
| Classic.Demo.Android | Release build produced signed APK; no device/emulator execution |
| Classic.Demo.iOS | Simulator-target managed compilation on Windows; no Xcode packaging or simulator execution |
| Notepad | Release build; encoding preservation, unsupported characters, dirty state, unsaved-document cancellation, atomic save, search/replace, edit commands and generated print HTML |
| AvaloniaExplorer | Release build; URI normalization/invalid input, toolbar selection and Internet Options tabs/OK; native WebView networking not tested by Headless |
| Classic.Avalonia.Tests | All 63 Release cases passed |

## Regression Fixes

- Nested TabControls own independent row and column definitions. Shared style definitions previously leaked the outer navigation's measured width into the inner left-side tab strip, leaving an 81 DIP gap in the demo. Tests cover the real demo, left/right nesting, selection changes and resizing.
- Menu popups anchor to the complete owning menu item instead of the inset template panel. Top-level popups no longer overlap the menu bar by one pixel. Minimum row heights allow larger text without overflowing; regression tests cover 11 and 24 DIP text with checked Word Wrap and Font entries.
- Default Tahoma uses grayscale antialiasing, Strong hinting and aligned baselines. Unsmoothed rendering remains opt-in. Menu and caption rendering options are covered by layout/lifecycle tests.
- Date/time wheel selections retain the theme highlight color and white text when focus moves to OK. Clicking their read-only value area opens the same dropdown as the arrow; right-click and disabled controls do not. CalendarDatePicker's text area remains editable.
- Date/time candidates show a light hover background without changing selection. Clicking selects and centers the value while keeping the popup open. OK commits, Cancel discards, and reopening restores the committed values; covered with real pointer input in Headless tests.
- Calendar selection no longer uses a hardcoded pale background. The demo calendar/date/time values start populated, and time-popup Cancel uses the normal text size.
- Demo button clicks and spinner events now use Crystal EventToCommand with explicit routed events, compiled command bindings and spin event arguments. The click example has a visible counter.
- The Buttons category arranges radio options in two columns instead of squeezing four labels into one row at narrow widths.
- Embedded Tahoma regular/bold restore the original UI typography at 11 DIPs. Font resolution, printable ASCII, both weights and TrueType printing embedding are tested.
- Colors now starts at the top-left with a red preview, wider component editors, an accurate HSV value label and an explicit two-way spectrum-slider binding.
- Demo now uses the real Crystal.Avalonia application/module/DI model. Views moved to `Views` and use absolute embedded icon URIs. The rebuilt Browser was checked again for startup, category switching and date popup opening/cancellation after this migration.
- The single-view root template lacked `PART_VisualLayerManager`. Avalonia 12 could not enable its popup overlay, making dropdown clicks fail in Browser. The new single-view test forces overlay popups rather than silently using native popup windows.
- Explorer now rejects malformed addresses without throwing, reports an address error, and accepts uppercase schemes and surrounding whitespace.
- Unchecking an Explorer toolbar radio option no longer overrides the newly selected option.
- Internet Options OK now closes the window.
- Responsive tests now process queued resize events before checking desktop navigation restoration.

## Remaining Limits

- Tahoma retains Microsoft's license, separate from MIT/OFL. Browser has no embedded CJK fallback; Chinese applications need a suitable licensed font. Desktop fallback is separate.
- Restore reports NU1903 for transitive `Tmds.DBus.Protocol` 0.90.3. Dependency remediation remains outstanding; this verification does not certify security.
- Headless tests exercise layout, binding and input but do not replace native window, WebView, printing, file-provider or device testing. Linux/macOS native behavior and complete iOS deployment remain unverified locally.
- Demo menus and Explorer's historical Internet Options UI include presentation-only sample commands/settings. They are not a complete editor or a persistent browser-settings implementation.

The repository's existing CI builds/tests on Windows, Linux and macOS and builds Browser, Android and iOS. That workflow's remote results were not run or inferred during this local verification.

## Desktop Style Comparison

Native Windows screenshots on 2026-10-10 were compared with the original repository screenshots and theme resources at `cd7c71e`. All 14 palettes retain their original color values; the classic border renderer and TabItem chrome remain unchanged. Menus, toolbars and the repaired nested side-tab layout retain the original classic style. Default grayscale Tahoma smoothing deliberately differs from the original unsmoothed text; this is not a pixel-identical Windows 98 recreation.

Local screenshots are saved under `artifacts/verification/current-navigation-desktop.png` and `current-menu-desktop.png`. The review used the desktop host; Browser/mobile verification remains compile-only.

## Combined Pages Site

The Browser Release publish and strict DocFX build passed. `Prepare-GitHubPages.ps1` assembled documentation at the site root and the preview at `demo/`. The generated preview retains .NET's fingerprinted JS/import-map assets; all four mapped entries matched their SHA-256 integrity values. The local document and preview entry URLs returned HTTP 200. No WebAssembly UI/runtime test was performed. Remote Pages deployment runs after pushing master with GitHub Actions enabled as the Pages source.

## Library NativeAOT

All three libraries passed strict AOT/trim analysis and Windows x64 NativeAOT publishing in two modes: all library members rooted for analysis, and normal application trimming without those roots. Both native executables passed 293 assertions with dynamic code unavailable. Coverage includes embedded fonts, 26 control templates, nested side-tab alignment, 14 palettes, picker commit/cancel behavior, color editing and four dialog types. Binding diagnostics fail the test; no blanket IL warning suppression is used.

```powershell
dotnet publish tests/Classic.Avalonia.AotTests/Classic.Avalonia.AotTests.csproj -c Release -r win-x64 -p:AnalyzeAllLibraryMembers=true -p:GeneratePackageOnBuild=false -o artifacts/aot/library-analysis
./artifacts/aot/library-analysis/Classic.Avalonia.AotTests.exe
dotnet publish tests/Classic.Avalonia.AotTests/Classic.Avalonia.AotTests.csproj -c Release -r win-x64 -p:GeneratePackageOnBuild=false -o artifacts/aot/library-trimmed
./artifacts/aot/library-trimmed/Classic.Avalonia.AotTests.exe
```

Use compiled XAML and compiled application bindings. Whole-library rooting is an analysis technique, not a consumer requirement. These results verify library compatibility; arbitrary consumer reflection, platform backends and static native linking remain separate responsibilities. CI includes both publishing modes; remote results were not inferred.

## Notepad NativeAOT

Notepad conditionally uses StaticLink.Avalonia 3.119.4-7922.1 for NativeAOT. Publish with `dotnet publish samples/Notepad/Notepad.csproj -p:PublishProfile=WinX64Aot -o artifacts/aot/notepad/single-file`, then run `pwsh -File scripts/Test-NotepadAot.ps1 -Executable artifacts/aot/notepad/single-file/Notepad.exe`.

Windows x64 publishing, isolated EXE startup, native PE format and loaded-module checks passed on 2026-10-10. The accessibility tree exposes the main menu, status and editor. Desktop automated input failed with GetCursorPos access denied (0x80070005); AOT editing/dialog interaction remains unverified. This is separate from the 58 Headless test cases. See the DocFX NativeAOT guide for scope and reproduction.
