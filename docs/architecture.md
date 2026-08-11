# Architecture

CursorRing keeps lifecycle management, persisted settings, rendering, native GCD access, and configuration UI separate. Runtime code contains no explanatory comments, so this document records the decisions that are not evident from names and control flow alone.

## Cursor ownership

The renderer uses Dalamud's plugin-scoped `IAddonEventManager` to force `AddonCursorType.Hidden` only while the replacement is visible. Every transition away from visible state resets the override. UI hiding, disposal, and rendering failures also reset it.

Dalamud implements cursor forcing as one shared override rather than a stack. Another cursor-forcing plugin can replace or reset CursorRing's override. CursorRing changes that override only when its visibility state changes.

The addon override controls `AtkCursor`, but world interaction cursors such as hand and search use the separate `Client.System.Input.Cursor` path. While CursorRing renders, it captures that cursor's visibility, writes it hidden at the end of every rendered frame, and restores the captured value on every hide, failure, logout, profile transition, and disposal path. A missing native cursor prevents taking ownership, and an unavailable pointer during restoration clears local ownership so the game can recover its state on its next update.

The ring uses the main viewport foreground draw list. It creates no overlay window and therefore never captures clicks.

## Visibility

Normal visibility is one of five explicit presets: always, combat, duty, duty combat, or combat or duty. Duty occupancy includes PvE and PvP content from entry through exit. Mouse-look applies a second policy because FFXIV captures the pointer during camera control. By default it follows normal visibility; combat-only mouse-look can narrow but never broaden the selected preset.

Duty occupancy is cached on construction, zone initialization, and logout. The render path combines it with Dalamud's combat condition before native cursor, cooldown, cast, or geometry work.

The FFXIV client cursor exposes viewport state through FFXIVClientStructs. Mouse-look is detected from the UI-filtered left- or right-button state that FFXIV passes to viewport handling. Window-level cursor capture is not used because it remains active during ordinary focused gameplay. If required native input state is unavailable, the renderer fails closed and returns cursor ownership to the game.

During mouse capture, FFXIV repeatedly recenters the underlying pointer. CursorRing freezes the ring at the last valid free-pointer position for the entire capture interval so those recentering coordinates cannot create visible jitter. It falls back to the viewport center when no prior position exists and revalidates the anchor after viewport changes.

Hover feedback never broadens normal cursor visibility. Its visibility policy intersects the already-visible cursor with whenever-visible, out-of-combat, or in-combat state, and mouse-look always suppresses it because the captured pointer does not represent a meaningful world hover.

FFXIV's target system maintains separate pointers for a world-model mouseover and a nameplate mouseover. CursorRing treats either as an interactable entity and reads those pointers directly through FFXIVClientStructs only when hover feedback is enabled and eligible. Dalamud's managed target properties create a game-object reference for each access, so they are not used in the steady-state render path. A missing target system or read failure produces a non-hovered result without hiding the otherwise valid cursor. Arbitrary UI controls are outside this feature's entity scope. See [FFXIVClientStructs TargetSystem](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/Control/TargetSystem.cs) and [Dalamud TargetManager](https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Game/ClientState/Objects/TargetManager.cs).

## Global cooldown

The GCD indicator is enabled by default. Disabling it hides every dependent setting, skips native GCD and cast reads, resets tracked cast state, and leaves cursor rendering otherwise unchanged.

FFXIV Action sheet cooldown group 58 maps to zero-based runtime recast group 57. CursorRing obtains its detail through `ActionManager.GetRecastGroupDetail`. `RecastDetail.Elapsed` increases toward `RecastDetail.Total`. CursorRing accepts either the native active flag or an advancing valid timer, rejects an idle zero timer when the flag is false, normalizes active progress to the zero-inclusive and one-exclusive range, and hides the indicator immediately at completion.

Fill mode draws the elapsed interval from twelve o'clock. Drain mode draws the remaining interval after the elapsed interval. Rotation changes the sign of a full turn.

## Cast segmentation

Segmentation is opt-in and builds a display timeline from the GCD start through the later of GCD completion or the projected live cast end. Ordinary casts retain casting, slidecast, and post-cast GCD recovery intervals. Long casts scale one complete ring to their full duration and continue after the native GCD timer ends, without an artificial recovery interval. The local player's live `CastInfo.CurrentCastTime` and `TotalCastTime` provide the realized duration after spell speed, haste, level sync, traits, procs, and class mechanics. Static Action-sheet cast times and `BaseCastTime` are not used. The cast start is latched as `gcd elapsed - cast elapsed`, so a small frame-order offset remains stable while the live total and normalized boundaries update immediately.

The tracker accepts casts only when the native primary or additional recast group is group 57 and the observed cast start aligns with the beginning of that GCD. Recast groups are resolved only for attachment; an accepted cast is then followed by source sequence from live `CastInfo` even after group 57 completes. An unavailable or unknown group fails closed. Instant casts never create a cast timeline. An interrupted or replaced cast clears immediately. A completed long cast disappears, while a committed ordinary cast retains its GCD recovery until group 57 ends.

FFXIVClientStructs documents the matching `CastInfo.ResponseSourceSequence` transition as the point when ActionEffect has been received, the cast can no longer be cancelled, and the slidecast window begins. The observed boundary is stored in elapsed seconds and renormalized whenever the effective duration changes. This is an exact client-observed transition, but it cannot be known before it arrives and may be observed later under network delay or packet loss. The source sequence is required to prevent a stale response from a prior cast being accepted.

Slidecast timing always starts by subtracting the configured grace duration from the current live total, so the normalized marker follows live duration changes. Once the matching response is observed, that prediction is replaced with the confirmed boundary.

The default prediction is 500 milliseconds, matching maintained FFXIV cast-bar addons, but it is an estimate rather than a universal safety guarantee. The live adjusted cast duration remains authoritative in every mode. See [FFXIVClientStructs CastInfo](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/Character/CastInfo.cs), [ActionManager](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/ActionManager.cs), [SimpleTweaks Cast Bar Adjustments](https://github.com/Caraxi/SimpleTweaksPlugin/blob/main/Tweaks/UiAdjustment/CastBarAdjustments.cs), and [DelvUI Castbar](https://github.com/DelvUI/DelvUI/blob/develop/DelvUI/Interface/GeneralElements/CastbarHud.cs).

Rendering intersects the semantic intervals with the currently visible fill or drain interval before converting them to angles. This keeps segment colors correct in both rotation directions and avoids per-segment progress state. Optional dividers are radial ticks for stroke rings and center-to-edge lines for pie overlays.

## Geometry

Inner and outer GCD radii derive from the main radius, both stroke widths, and configured spacing. When an inner ring cannot fit at the requested thickness and spacing, its effective geometry contracts to remain fully inside the main ring. A pie overlay uses the main ring's inner edge and is rendered as a triangle fan so sectors larger than 180 degrees remain valid.

Optional circle and dot outlines render immediately beneath their corresponding foreground shapes. The circle outline reuses the main radius with a wider stroke, while the dot outline uses a larger filled radius. Both retain independent thickness and color settings.

All cursor layers share one whole-pixel-snapped center so the horizontal and vertical axes use the same raster phase. Hover indicators use a fixed number of direct line primitives. Crosshairs use four radial strokes, inward carets and corner brackets use four connected three-point polylines, and every style shares one per-frame rotation basis. Caret arms use equal radial depth and tangential half-width, producing symmetric 45-degree arms with a consistent joined apex in every orientation. Position starts outside the normal visible dot extent, including its enabled outline. Hover can independently replace the ring and dot foreground colors or suppress the complete dot and its outline, while GCD rendering and cast segments retain their normal colors.

The main circle, center dot, and GCD indicator each have a one-pixel black outline enabled by default. Stroke presentations use a wider under-stroke. With a background track, the GCD outline covers the complete second ring; without one, it follows only the visible fill or drain interval. Pie presentation uses an inset foreground over a border-colored sector or disk. Inner and outer ring placement includes enabled main-ring and GCD outlines when preserving configured spacing. An oversized inner outline contracts with the foreground stroke so the complete second ring stays between the center and the main ring.

## Profiles and assignments

The root configuration remains the permanent Default profile so existing configuration files retain their settings without migration. Named profiles own complete deep-copied settings snapshots and do not inherit or merge values. Profile identifiers and names are repaired during normalization, nested settings are normalized independently, and assignments with malformed targets or missing profiles are discarded.

Instance data is catalogued into disjoint domains. Named non-PvP content-finder rows form the PvE duty catalog. Named territories outside duties and PvP form the PvE zone catalog. Territories marked for PvP or referenced by PvP content-finder rows form the PvP catalog, including Wolves' Den. Territory variants sharing a place-name row share one zone or PvP target, while equivalent PvE duty rows share the first row identifier for their localized duty name and territory.

Assignments are cached in one dictionary keyed by scope and target. PvP resolves a specific PvP location, PvP Any, then Global Default. A PvE duty resolves a specific duty, Duty Any, then Global Default. An ordinary zone resolves a specific zone, then Global Default. These domains never fall through into each other, and an empty profile identifier explicitly selects the permanent Default. Construction and zone initialization use client state plus catalog metadata, while PvP enter and leave events update the active domain at runtime.

The active settings reference is cached. Zone initialization supplies both the territory and content-finder row, while initial construction also reads the current duty state so loading inside content resolves immediately. A changed active reference resets cursor ownership and cast tracking before another render. The renderer reads that reference once at the start of each frame and performs no assignment collection work.

## Configuration interface

The settings window separates complete profile editing from location assignments. Default is permanent, while named profiles can be created, duplicated, renamed, previewed, reset, and deleted with their assignments. Any profile can be selected as the unassigned-location default, with the permanent Default used if that selection becomes invalid. Profile actions occupy a stable toolbar. The profile editor keeps a full-height preview beside an independently scrolling editor divided into Cursor, GCD, and Cast timing domains. Compact selects preserve scanability, conditional controls remain visible when relevant, and colors rely on the built-in picker. Benchmark controls occupy a separate build-gated tab.

The preview pane stacks equal-height Normal and Hover regions. Both use the same simulated GCD and cast timing and a shared scale based on the larger visual extent. Normal forces hover off; Hover forces it on when enabled and otherwise identifies itself as disabled. This provides a stable comparison without requiring a live game target under the settings window. The outer configuration window and preview never scroll; scrolling is confined to the profile-settings and assignment children, and the minimum window height keeps both previews usable.

Assignments use a bounded mapping table with Scope, Location, and Profile columns. Zone, Duty, and PvP scopes expose separate selector datasets; Duty and PvP also offer Any. A permanent unsaved row defaults to PvP Any in PvP, the current PvE duty in content, and the current PvE zone otherwise. Existing rows remain directly editable, duplicate targets are unavailable, and searches are rendered only while their selector is open. Category defaults display first, followed by specific PvP locations, PvE duties, and PvE zones. Legacy PvP-duty assignments migrate into the PvP domain, known duty or PvP territory assignments are removed from the Zone domain, and unknown identifiers remain removable.

Every value control has a persistent sentence-case label and visible unit. Labels occupy a consistent compact desktop column while values use the remaining width.

Checkboxes enable optional features and reveal only their directly related controls. Short helper text is reserved for behavior that a concise label cannot explain, such as the background track and predicted versus confirmed slidecast timing. Changes save immediately, and the single reset action states its full scope. This follows the form-label, helper-text, grouping, and conditional-disclosure guidance in the [Carbon Design System](https://preview.carbondesignsystem.com/building-blocks/core/patterns/forms) and [GOV.UK Design System](https://design-system.service.gov.uk/components/radios/).

## Failure behavior

Invalid configuration values are normalized on load and before save. Invalid native GCD values produce an inactive GCD. Any rendering exception restores the native cursor and is logged once until rendering succeeds again.

## Performance benchmark

Benchmark support is compiled only when `CURSORRING_BENCHMARK` is defined. Debug and optimized Benchmark builds define it; Release builds do not. CI scans the Release assembly for multiple benchmark-only metadata and user-string markers so accidental inclusion fails the build.

Release rendering has no benchmark branch, collector, interface, timing call, or allocation measurement call. In a Debug or Benchmark build, an explicitly started benchmark presents a three-second preparation countdown, then wraps only the cursor renderer for ten seconds and records wall-clock render-path duration, current-thread allocations, and the change in ImGui vertex and index counts. Collection uses a preallocated sample buffer. Percentile calculation and text formatting occur after collection.

The benchmark excludes the configuration window and Dalamud's later batched GPU submission. Vertex and index counts are therefore reported as the stable proxy for GPU work. Elapsed timings can include operating-system preemption and include the small cost of reading ImGui buffer sizes, so they are conservative measurements rather than isolated CPU execution time or total frame time.

Run the optimized Benchmark build while the ring is visible, hover a targetable entity, and repeatedly trigger casted GCD actions. The benchmark reports active-GCD, cast-segmented, and hovered sample counts, while the configuration window shows the latest raw recast timer observation. Test the largest pie configuration with both outlines, segment dividers, and corner-bracket hover feedback enabled as the geometry-heavy case. A healthy steady-state result should report zero managed allocations per rendered frame and a mean share comfortably below one percent of a 144 Hz frame budget. Percentile and maximum timings remain sensitive to system scheduling and should be compared across several runs.
