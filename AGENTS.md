# CursorRing GenAI Guidelines

These instructions apply to every AI-assisted change in this repository, regardless of tool or model.

## Priorities

- Keep the plugin minimal, readable, safe, and allocation-free during steady-state rendering.
- Preserve existing user changes. Never discard, overwrite, or reformat unrelated work.
- Prefer supported Dalamud and current FFXIVClientStructs APIs. Fail closed when native state is unavailable or invalid.
- Do not claim native behavior is verified until it has been tested in game.

## Structure

- Keep lifecycle, cursor ownership, rendering, native readers, settings, and configuration UI separate.
- Put persisted options and defaults in `CursorSettings.cs` and normalize every scalar, enum, and color.
- Do not bump the configuration version unless a genuinely incompatible migration is required.
- Keep benchmark code behind `CURSORRING_BENCHMARK`. Release assemblies must contain no benchmark code or strings.

## Code style

- Target the repository's pinned Dalamud SDK and latest .NET supported by it.
- Write compact, descriptive C# with small methods and no unnecessary abstraction.
- Add zero comments to production or test C#. Put rationale and source links in `docs/architecture.md`.
- Avoid LINQ, per-frame collections, closures, strings, or other allocations in render and native-read paths.
- Validate pointers, finite numbers, ranges, lifecycle transitions, and cursor restoration paths.
- Keep ImGui controls clearly labeled, include units, group related settings, and reveal optional fields only when enabled.

## Performance-oriented design

- Keep every per-frame path bounded, allocation-free, and independent of collection size.
- Disabled features must add no native reads, geometry, formatting, logging, or avoidable state updates.
- Read native state only when needed. Cache stable values, latch event boundaries, and update services on transitions instead of every frame.
- Reuse ImGui draw lists and direct primitives. Do not add input windows, textures, timers, background workers, or retained segment objects without measured need.
- Keep geometry deterministic and capped. Prefer a few explicit value-type operations over arrays, LINQ, delegates, or generalized pipelines.
- Move diagnostics, timing, allocation probes, and result formatting behind `CURSORRING_BENCHMARK`.
- Benchmark both the default path and the most expensive enabled configuration. Require zero steady-state managed allocations and investigate regressions before accepting them.
- Prefer simple measured code over speculative micro-optimizations; record non-obvious performance decisions in `docs/architecture.md`.

## Behavior invariants

- Hide the native cursor only while CursorRing is visibly rendered; always restore it on hide, error, logout, and disposal.
- Render through the foreground draw list without an input-capturing window.
- Keep mouse-look positioning stable and never consume captured pointer noise as an absolute position.
- Read GCD group 57 through the current native accessor and reject invalid or completed timers.
- Use live adjusted cast timing. Accept cast segmentation only after a positive GCD-group match and match responses by source sequence.

## Verification

Run before handing off a code change:

```powershell
dotnet format CursorRing.sln --verify-no-changes --no-restore
dotnet build CursorRing.sln --configuration Debug --no-restore --property:Platform=x64
dotnet build CursorRing.sln --configuration Benchmark --no-restore --property:Platform=x64
dotnet build CursorRing.sln --configuration Release --no-restore --property:Platform=x64
dotnet test CursorRing.Tests\CursorRing.Tests.csproj --configuration Release --no-build --no-restore --property:Platform=x64
git diff --check
```

- Add focused tests for pure timing, geometry, normalization, lifecycle, and benchmark math.
- Confirm production and test C# contain no comment tokens.
- Manually test native cursor ownership, combat transitions, mouse-look, GCD/cast timing, focus loss, UI hiding, zoning, logout, and unload in game.

## Git

- Do not commit generated output or local tooling files.
- Make focused commits only when explicitly requested.
