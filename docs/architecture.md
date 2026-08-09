# Architecture

CursorRing keeps lifecycle management, persisted settings, rendering, native GCD access, and configuration UI separate. Runtime code contains no explanatory comments, so this document records the decisions that are not evident from names and control flow alone.

## Cursor ownership

The renderer uses Dalamud's plugin-scoped `IAddonEventManager` to force `AddonCursorType.Hidden` only while the replacement is visible. Every transition away from visible state resets the override. UI hiding, disposal, and rendering failures also reset it.

Dalamud implements cursor forcing as one shared override rather than a stack. Another cursor-forcing plugin can replace or reset CursorRing's override. CursorRing minimizes interference by changing the override only when its own visibility state changes.

The ring uses the main viewport foreground draw list. It creates no overlay window and therefore never captures clicks.

## Visibility

Normal visibility is either always or combat-only. Mouse-look applies a second policy because FFXIV captures the pointer during camera control. The default mouse-look policy shows the ring only in combat.

The FFXIV client cursor exposes viewport state through FFXIVClientStructs. Mouse-look is detected from the UI-filtered left- or right-button state that FFXIV passes to viewport handling. Window-level cursor capture is not used because it remains active during ordinary focused gameplay. If required native input state is unavailable, the renderer fails closed and returns cursor ownership to the game.

During mouse capture, FFXIV repeatedly recenters the underlying pointer. CursorRing freezes the ring at the last valid free-pointer position for the entire capture interval so those recentering coordinates cannot create visible jitter. It falls back to the viewport center when no prior position exists and revalidates the anchor after viewport changes.

## Global cooldown

The GCD indicator is enabled by default. Disabling it hides every dependent setting, skips native GCD and cast reads, resets tracked cast state, and leaves cursor rendering otherwise unchanged.

FFXIV Action sheet cooldown group 58 maps to zero-based runtime recast group 57. CursorRing obtains its detail through `ActionManager.GetRecastGroupDetail`. `RecastDetail.Elapsed` increases toward `RecastDetail.Total`. CursorRing accepts either the native active flag or an advancing valid timer, rejects an idle zero timer when the flag is false, normalizes active progress to the zero-inclusive and one-exclusive range, and hides the indicator immediately at completion.

Fill mode draws the elapsed interval from twelve o'clock. Drain mode draws the remaining interval after the elapsed interval. Rotation changes the sign of a full turn.

## Cast segmentation

Segmentation is opt-in and divides one GCD timeline into casting, slidecast, and post-cast recovery intervals. The local player's live `CastInfo.CurrentCastTime` and `TotalCastTime` provide the realized duration after spell speed, haste, and class mechanics. Static Action-sheet cast times and `BaseCastTime` are not used. The cast start is aligned to the GCD as `gcd elapsed - cast elapsed`, so a one-frame update offset does not shift the end marker.

The tracker accepts casts only when the native primary or additional recast group is group 57 and the observed cast start aligns with the beginning of that GCD. An unavailable or unknown group fails closed. Instant casts never create a cast timeline. A cast that ends without a matching response is treated as interrupted; a committed timeline remains visible until its GCD ends.

FFXIVClientStructs documents the matching `CastInfo.ResponseSourceSequence` transition as the point when ActionEffect has been received, the cast can no longer be cancelled, and the slidecast window begins. This is an exact client-observed transition, but it cannot be known before it arrives and may be observed later under network delay or packet loss. The source sequence is required to prevent a stale response from a prior cast being accepted.

The three timing modes make that distinction explicit:

- Predicted uses a stable configurable grace window and never moves the marker.
- Confirmed only shows slidecast time after the matching response is observed.
- Hybrid starts with the prediction and replaces it once with the observed threshold.

The default prediction is 500 milliseconds, matching maintained FFXIV cast-bar addons, but it is an estimate rather than a universal safety guarantee. The live adjusted cast duration remains authoritative in every mode. See [FFXIVClientStructs CastInfo](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/Character/CastInfo.cs), [ActionManager](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/ActionManager.cs), [SimpleTweaks Cast Bar Adjustments](https://github.com/Caraxi/SimpleTweaksPlugin/blob/main/Tweaks/UiAdjustment/CastBarAdjustments.cs), and [DelvUI Castbar](https://github.com/DelvUI/DelvUI/blob/develop/DelvUI/Interface/GeneralElements/CastbarHud.cs).

Rendering intersects the semantic intervals with the currently visible fill or drain interval before converting them to angles. This keeps segment colors correct in both rotation directions and avoids per-segment progress state. Optional dividers are radial ticks for stroke rings and center-to-edge lines for pie overlays.

## Geometry

Inner and outer GCD radii derive from the main radius, both stroke widths, and configured spacing. When an inner ring cannot fit at the requested thickness and spacing, its effective geometry contracts to remain fully inside the main ring. A pie overlay uses the main ring's inner edge and is rendered as a triangle fan so sectors larger than 180 degrees remain valid.

Optional circle and dot outlines render immediately beneath their corresponding foreground shapes. The circle outline reuses the main radius with a wider stroke, while the dot outline uses a larger filled radius. Both are disabled by default and retain independent thickness and color settings.

The GCD indicator has its own disabled-by-default outline. Stroke presentations use a wider under-stroke. With a background track, the outline covers the complete second ring; without one, it follows only the visible fill or drain interval. Pie presentation uses an inset foreground over a border-colored sector or disk. Inner and outer ring placement includes enabled main-ring and GCD outlines when preserving configured spacing. An oversized inner outline contracts with the foreground stroke so the complete second ring stays between the center and the main ring.

## Configuration interface

The settings window is one scrolling form with a live preview followed by visibility, cursor appearance, GCD, and cast timing sections, then a clearly scoped reset action. Every value control has a persistent sentence-case label and visible unit. Labels occupy a consistent compact desktop column while values use the remaining width.

Checkboxes enable optional features and reveal only their directly related controls. Short helper text is reserved for behavior that a concise label cannot explain, such as the background track and predicted versus confirmed slidecast timing. Changes save immediately, and the single reset action states its full scope. This follows the form-label, helper-text, grouping, and conditional-disclosure guidance in the [Carbon Design System](https://preview.carbondesignsystem.com/building-blocks/core/patterns/forms) and [GOV.UK Design System](https://design-system.service.gov.uk/components/radios/).

## Failure behavior

Invalid configuration values are normalized on load and before save. Invalid native GCD values produce an inactive GCD. Any rendering exception restores the native cursor and is logged once until rendering succeeds again.

## Performance benchmark

Benchmark support is compiled only when `CURSORRING_BENCHMARK` is defined. Debug and optimized Benchmark builds define it; Release builds do not. CI scans the Release assembly for multiple benchmark-only metadata and user-string markers so accidental inclusion fails the build.

Release rendering has no benchmark branch, collector, interface, timing call, or allocation measurement call. In a Debug or Benchmark build, an explicitly started benchmark presents a three-second preparation countdown, then wraps only the cursor renderer for ten seconds and records wall-clock render-path duration, current-thread allocations, and the change in ImGui vertex and index counts. Collection uses a preallocated sample buffer. Percentile calculation and text formatting occur after collection.

The benchmark excludes the configuration window and Dalamud's later batched GPU submission. Vertex and index counts are therefore reported as the stable proxy for GPU work. Elapsed timings can include operating-system preemption and include the small cost of reading ImGui buffer sizes, so they are conservative measurements rather than isolated CPU execution time or total frame time.

Run the optimized Benchmark build while the ring is visible and repeatedly trigger casted GCD actions. The benchmark reports active-GCD and cast-segmented sample counts, while the configuration window shows the latest raw recast timer observation. Test the largest pie configuration with both outlines and segment dividers enabled as the geometry-heavy case. A healthy steady-state result should report zero managed allocations per rendered frame and a mean share comfortably below one percent of a 144 Hz frame budget. Percentile and maximum timings remain sensitive to system scheduling and should be compared across several runs.
