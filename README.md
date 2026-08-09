# CursorRing

[![CursorRing CI](https://github.com/ptMuta/CursorRing/actions/workflows/pr-build.yml/badge.svg?branch=master)](https://github.com/ptMuta/CursorRing/actions/workflows/pr-build.yml)

CursorRing is a Dalamud plugin that replaces the Final Fantasy XIV cursor with a configurable hollow circle and center dot. It can also show global cooldown progress around or inside the cursor.

## Features

- Always-visible or combat-only cursor replacement
- Configurable behavior during camera mouse-look
- Highly customisable
- Optional GCD ring
- Optional cast, slidecast, and post-cast GCD segments with independent colors

## Installation

Build `CursorRing.sln`, then add the resulting `CursorRing.dll` as a Dalamud development plugin through `/xlsettings`.

The Debug output is located at `CursorRing/bin/x64/Debug/CursorRing.dll`. Open the plugin settings with `/cursorring` or the configuration button in `/xlplugins`.

Debug and Benchmark builds provide `/cursorring benchmark` and a settings button for collecting a 10-second benchmark after a visible three-second countdown. Benchmark builds are optimized and produce representative timing results at `CursorRing/bin/x64/Benchmark/CursorRing.dll`. Results include render-path elapsed time, managed allocations, ImGui geometry, active-GCD and cast-segment samples, and the share of a 144 Hz frame budget. Release builds exclude the collector, command path, interface, telemetry, and result types at compile time.

## Development

Requirements:

- XIVLauncher and Dalamud
- .NET 10 SDK

Run the verification suite with:

```powershell
dotnet restore CursorRing.sln
dotnet build CursorRing.sln --configuration Debug --no-restore
dotnet build CursorRing.sln --configuration Benchmark --no-restore
dotnet build CursorRing.sln --configuration Release --no-restore
dotnet test CursorRing.Tests\CursorRing.Tests.csproj --configuration Release --property:Platform=x64 --no-build --no-restore
dotnet format CursorRing.sln --verify-no-changes --no-restore
```

Technical documents in [docs/architecture.md](docs/architecture.md).

## AI usage

Generative AI tooling was used during implementation. For primary guidance document for GenAI see [AGENTS.md](AGENTS.md).

## License

CursorRing is licensed under AGPL-3.0-or-later. See [LICENSE.md](LICENSE.md).
