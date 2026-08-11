# CursorRing

[![CI](https://github.com/ptMuta/CursorRing/actions/workflows/pr-build.yml/badge.svg?branch=master)](https://github.com/ptMuta/CursorRing/actions/workflows/pr-build.yml)

CursorRing is a Dalamud plugin that replaces the Final Fantasy XIV cursor with a configurable hollow circle and center dot. It can also show global cooldown progress around or inside the cursor.

## Features

- Always, combat, duty, duty-combat, or combat-or-duty visibility
- Separate profile assignments for PvE zones, PvE duties, and PvP locations
- Configurable behavior during camera mouse-look
- Interactable-entity hover feedback with crosshair, inward-caret, or corner-bracket styles
- Highly customisable
- Optional GCD ring
- Optional cast, slidecast, and post-cast GCD segments with independent colors

## Installation

In `/xlsettings`, open `Experimental`, add this URL under `Custom Plugin Repositories`, and save:

```text
https://raw.githubusercontent.com/ptMuta/CursorRing/master/repo.json
```

Open `/xlplugins`, search for `CursorRing`, and select `Install`.

To use a development build, build `CursorRing.sln`, then add the resulting `CursorRing/bin/x64/Debug/CursorRing.dll` as a Dalamud development plugin through `/xlsettings`.

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
