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

FFXIV Action sheet cooldown group 58 maps to zero-based runtime recast group 57. CursorRing obtains its detail through `ActionManager.GetRecastGroupDetail`. `RecastDetail.Elapsed` increases toward `RecastDetail.Total`. CursorRing accepts either the native active flag or an advancing valid timer, rejects an idle zero timer when the flag is false, normalizes active progress to the zero-inclusive and one-exclusive range, and hides the indicator immediately at completion.

Fill mode draws the elapsed interval from twelve o'clock. Drain mode draws the remaining interval after the elapsed interval. Rotation changes the sign of a full turn.

## Geometry

Inner and outer GCD radii derive from the main radius, both stroke widths, and configured spacing. When an inner ring cannot fit at the requested thickness and spacing, its effective geometry contracts to remain fully inside the main ring. A pie overlay uses the main ring's inner edge and is rendered as a triangle fan so sectors larger than 180 degrees remain valid.

Optional circle and dot outlines render immediately beneath their corresponding foreground shapes. The circle outline reuses the main radius with a wider stroke, while the dot outline uses a larger filled radius. Both are disabled by default and retain independent thickness and color settings.

## Failure behavior

Invalid configuration values are normalized on load and before save. Invalid native GCD values produce an inactive GCD. Any rendering exception restores the native cursor and is logged once until rendering succeeds again.

## Performance benchmark

Benchmark support is compiled only when `CURSORRING_BENCHMARK` is defined. Debug and optimized Benchmark builds define it; Release builds do not. CI scans the Release assembly for multiple benchmark-only metadata and user-string markers so accidental inclusion fails the build.

Release rendering has no benchmark branch, collector, interface, timing call, or allocation measurement call. In a Debug or Benchmark build, an explicitly started benchmark presents a three-second preparation countdown, then wraps only the cursor renderer for ten seconds and records wall-clock render-path duration, current-thread allocations, and the change in ImGui vertex and index counts. Collection uses a preallocated sample buffer. Percentile calculation and text formatting occur after collection.

The benchmark excludes the configuration window and Dalamud's later batched GPU submission. Vertex and index counts are therefore reported as the stable proxy for GPU work. Elapsed timings can include operating-system preemption and include the small cost of reading ImGui buffer sizes, so they are conservative measurements rather than isolated CPU execution time or total frame time.

Run the optimized Benchmark build while the ring is visible and repeatedly trigger the GCD. The benchmark reports active-GCD sample count and the configuration window shows the latest raw recast timer observation. Test the largest pie configuration separately as the geometry-heavy case. A healthy steady-state result should report zero managed allocations per rendered frame and a mean share comfortably below one percent of a 144 Hz frame budget. Percentile and maximum timings remain sensitive to system scheduling and should be compared across several runs.
