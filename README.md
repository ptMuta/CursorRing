# CursorRing

CursorRing is a Dalamud plugin that replaces the Final Fantasy XIV cursor with a configurable hollow circle and center dot. It can also show global cooldown progress around or inside the cursor.

## Features

- Always-visible or combat-only cursor replacement
- Configurable behavior during camera mouse-look
- Adjustable circle, dot, GCD sizes, spacing, colors, and opacity
- Optional independent circle and dot outlines with configurable thickness and color
- Inner, outer, stroke-overlay, and pie-overlay GCD presentations
- Fill or drain progress in either rotation direction
- Optional GCD background track
- Input-transparent rendering
- Debug/Benchmark-only runtime performance benchmark

## Installation

Build `CursorRing.sln`, then add the resulting `CursorRing.dll` as a Dalamud development plugin through `/xlsettings`.

The Debug output is located at `CursorRing/bin/x64/Debug/CursorRing.dll`. Open the plugin settings with `/cursorring` or the configuration button in `/xlplugins`.

Debug and Benchmark builds provide `/cursorring benchmark` and a settings button for collecting a 10-second benchmark after a visible three-second countdown. Benchmark builds are optimized and produce representative timing results at `CursorRing/bin/x64/Benchmark/CursorRing.dll`. Results include render-path elapsed time, managed allocations, ImGui geometry, active-GCD samples, and the share of a 144 Hz frame budget. Release builds exclude the collector, command path, interface, telemetry, and result types at compile time.

## Development

Requirements:

- .NET 10 SDK
- XIVLauncher and Dalamud
- A local Dalamud development installation at the standard XIVLauncher path, or `DALAMUD_HOME` pointing to one

Run the verification suite with:

```powershell
dotnet restore CursorRing.sln
dotnet build CursorRing.sln --configuration Debug --no-restore
dotnet build CursorRing.sln --configuration Benchmark --no-restore
dotnet build CursorRing.sln --configuration Release --no-restore
dotnet test CursorRing.Tests\CursorRing.Tests.csproj --configuration Release --property:Platform=x64 --no-build --no-restore
dotnet format CursorRing.sln --verify-no-changes --no-restore
```

Production and test C# files intentionally contain no comments. Technical rationale is documented in [docs/architecture.md](docs/architecture.md).

## In-game verification

Native game interaction requires manual testing. Verify combat transitions, camera mouse-look, hardware and software cursor modes, focus loss, viewport exit, UI hiding, cutscenes, GPose, zoning, logout, and plugin unload. Confirm that the normal cursor returns whenever CursorRing is not visible.

Dalamud exposes cursor replacement as one shared override. Do not run CursorRing alongside another plugin that forces or replaces the game cursor.

## AI usage

AI tooling was used during implementation. Any submission to the official Dalamud plugin repository must be reviewed and substantially validated by a human, and must follow the current Dalamud AI usage and disclosure policy.

## License

CursorRing is licensed under AGPL-3.0-or-later. See [LICENSE.md](LICENSE.md).
