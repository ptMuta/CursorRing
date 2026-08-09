using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace CursorRing.Windows;

internal sealed class ConfigWindow : Window
{
    private readonly Configuration configuration;
#if CURSORRING_BENCHMARK
    private readonly RenderBenchmark benchmark;
#endif

    internal ConfigWindow(Configuration configuration)
        : base("CursorRing Settings###CursorRingConfig")
    {
        this.configuration = configuration;
#if CURSORRING_BENCHMARK
        benchmark = new RenderBenchmark();
#endif
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420f, 520f),
            MaximumSize = new Vector2(640f, 900f)
        };
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("Preview");
        ImGui.Separator();
        DrawPreview();
        DrawSection("Visibility", "Choose when CursorRing replaces the game cursor.");
        var changed = false;
        if (BeginForm("visibility"))
        {
            changed |= DrawEnum("visibility_mode", "Cursor visibility", configuration.Visibility, VisibilityLabel, value => configuration.Visibility = value);
            changed |= DrawEnum("mouse_look", "Mouse-look visibility", configuration.MouseLook, MouseLookLabel, value => configuration.MouseLook = value);
            ImGui.EndTable();
        }

        DrawSection("Cursor appearance", "Sizes are measured in screen pixels.");
        if (BeginForm("cursor"))
        {
            changed |= DrawFloat("circle_diameter", "Circle diameter", configuration.RingDiameter, 8f, 240f, "%.0f px", value => configuration.RingDiameter = value);
            changed |= DrawFloat("circle_thickness", "Circle thickness", configuration.RingThickness, 1f, 20f, "%.1f px", value => configuration.RingThickness = value);
            changed |= DrawColor("circle_color", "Circle color", configuration.RingColor, value => configuration.RingColor = value);
            changed |= DrawFloat("dot_diameter", "Dot diameter", configuration.DotDiameter, 1f, 64f, "%.1f px", value => configuration.DotDiameter = value);
            changed |= DrawColor("dot_color", "Dot color", configuration.DotColor, value => configuration.DotColor = value);
            ImGui.EndTable();
        }

        var showRingBorder = configuration.ShowRingBorder;
        if (ImGui.Checkbox("Circle outline", ref showRingBorder))
        {
            configuration.ShowRingBorder = showRingBorder;
            changed = true;
        }
        if (configuration.ShowRingBorder)
        {
            ImGui.Indent();
            if (BeginForm("circle_outline"))
            {
                changed |= DrawFloat("circle_outline_thickness", "Thickness", configuration.RingBorderThickness, 1f, 20f, "%.1f px", value => configuration.RingBorderThickness = value);
                changed |= DrawColor("circle_outline_color", "Color", configuration.RingBorderColor, value => configuration.RingBorderColor = value);
                ImGui.EndTable();
            }
            ImGui.Unindent();
        }

        var showDotBorder = configuration.ShowDotBorder;
        if (ImGui.Checkbox("Dot outline", ref showDotBorder))
        {
            configuration.ShowDotBorder = showDotBorder;
            changed = true;
        }
        if (configuration.ShowDotBorder)
        {
            ImGui.Indent();
            if (BeginForm("dot_outline"))
            {
                changed |= DrawFloat("dot_outline_thickness", "Thickness", configuration.DotBorderThickness, 1f, 20f, "%.1f px", value => configuration.DotBorderThickness = value);
                changed |= DrawColor("dot_outline_color", "Color", configuration.DotBorderColor, value => configuration.DotBorderColor = value);
                ImGui.EndTable();
            }
            ImGui.Unindent();
        }

        DrawSection("Global cooldown", "Shown only while the global cooldown is active.");
        var showGcd = configuration.ShowGcd;
        if (ImGui.Checkbox("GCD indicator", ref showGcd))
        {
            configuration.ShowGcd = showGcd;
            changed = true;
        }
        if (configuration.ShowGcd)
        {
            if (BeginForm("gcd"))
            {
                changed |= DrawEnum("gcd_placement", "Placement", configuration.GcdPlacement, GcdPlacementLabel, value => configuration.GcdPlacement = value);
                if (configuration.GcdPlacement == GcdPlacement.Overlay)
                {
                    changed |= DrawEnum("overlay_style", "Overlay style", configuration.OverlayFill, OverlayFillLabel, value => configuration.OverlayFill = value);
                }
                else
                {
                    changed |= DrawFloat("gcd_thickness", "Ring thickness", configuration.GcdThickness, 1f, 20f, "%.1f px", value => configuration.GcdThickness = value);
                    changed |= DrawFloat("gcd_spacing", "Gap from cursor ring", configuration.GcdSpacing, 0f, 40f, "%.1f px", value => configuration.GcdSpacing = value);
                }

                changed |= DrawEnum("progress_behavior", "Progress behavior", configuration.ProgressBehavior, ProgressBehaviorLabel, value => configuration.ProgressBehavior = value);
                changed |= DrawEnum("rotation_direction", "Rotation direction", configuration.Rotation, RotationLabel, value => configuration.Rotation = value);
                changed |= DrawColor("gcd_color", "GCD / post-cast color", configuration.GcdColor, value => configuration.GcdColor = value);
                ImGui.EndTable();
            }

            var showTrack = configuration.ShowGcdTrack;
            if (ImGui.Checkbox("Background track", ref showTrack))
            {
                configuration.ShowGcdTrack = showTrack;
                changed = true;
            }
            if (configuration.ShowGcdTrack)
            {
                ImGui.Indent();
                DrawHint("Shows the complete GCD path behind the moving progress.");
                if (BeginForm("gcd_track"))
                {
                    changed |= DrawColor("track_color", "Color", configuration.GcdTrackColor, value => configuration.GcdTrackColor = value);
                    ImGui.EndTable();
                }
                ImGui.Unindent();
            }

            var showGcdBorder = configuration.ShowGcdBorder;
            if (ImGui.Checkbox("GCD outline", ref showGcdBorder))
            {
                configuration.ShowGcdBorder = showGcdBorder;
                changed = true;
            }
            if (configuration.ShowGcdBorder)
            {
                ImGui.Indent();
                if (BeginForm("gcd_outline"))
                {
                    changed |= DrawFloat("gcd_outline_thickness", "Thickness", configuration.GcdBorderThickness, 1f, 20f, "%.1f px", value => configuration.GcdBorderThickness = value);
                    changed |= DrawColor("gcd_outline_color", "Color", configuration.GcdBorderColor, value => configuration.GcdBorderColor = value);
                    ImGui.EndTable();
                }
                ImGui.Unindent();
            }

            DrawSection("Cast timing", "Optionally divide casted GCD actions into casting, slidecast, and post-cast segments.");
            var showCastSegments = configuration.ShowCastSegments;
            if (ImGui.Checkbox("Cast timing segments", ref showCastSegments))
            {
                configuration.ShowCastSegments = showCastSegments;
                changed = true;
            }
            if (configuration.ShowCastSegments)
            {
                ImGui.Indent();
                DrawHint("Cast end uses the live adjusted duration. Instant and off-GCD actions remain unsegmented.");
                if (BeginForm("cast_timing"))
                {
                    changed |= DrawEnum("slidecast_timing", "Timing source", configuration.SlidecastTiming, SlidecastTimingLabel, value => configuration.SlidecastTiming = value);
                    if (configuration.SlidecastTiming != SlidecastTimingMode.Confirmed)
                    {
                        changed |= DrawFloat("predicted_grace", "Predicted grace window", configuration.SlidecastPredictionMilliseconds, 0f, 1000f, "%.0f ms", value => configuration.SlidecastPredictionMilliseconds = value);
                    }

                    changed |= DrawColor("casting_color", "Casting color", configuration.CastSegmentColor, value => configuration.CastSegmentColor = value);
                    changed |= DrawColor("slidecast_color", "Slidecast color", configuration.SlidecastSegmentColor, value => configuration.SlidecastSegmentColor = value);
                    ImGui.EndTable();
                }

                DrawHint(SlidecastTimingDescription(configuration.SlidecastTiming));
                var showDividers = configuration.ShowSegmentDividers;
                if (ImGui.Checkbox("Segment dividers", ref showDividers))
                {
                    configuration.ShowSegmentDividers = showDividers;
                    changed = true;
                }
                if (configuration.ShowSegmentDividers)
                {
                    ImGui.Indent();
                    if (BeginForm("segment_dividers"))
                    {
                        changed |= DrawFloat("divider_thickness", "Thickness", configuration.SegmentDividerThickness, 1f, 10f, "%.1f px", value => configuration.SegmentDividerThickness = value);
                        changed |= DrawColor("divider_color", "Color", configuration.SegmentDividerColor, value => configuration.SegmentDividerColor = value);
                        ImGui.EndTable();
                    }
                    ImGui.Unindent();
                }
                ImGui.Unindent();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        if (ImGui.Button("Reset all settings"))
        {
            changed |= configuration.Reset();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("Changes save automatically");

#if CURSORRING_BENCHMARK
        DrawSection("Performance benchmark", "Available only in Debug and Benchmark builds.");
        DrawBenchmark();
#endif

        if (changed)
        {
            configuration.Save();
        }
    }

#if CURSORRING_BENCHMARK
    internal RenderBenchmark Benchmark => benchmark;

    private void DrawBenchmark()
    {
        DrawHint("Measures the cursor render path for 10 seconds. Keep the ring visible for representative results.");

        if (benchmark.Phase == BenchmarkPhase.Countdown)
        {
            ImGui.ProgressBar((float)benchmark.CountdownProgress, new Vector2(-1f, 0f), $"Starting in {benchmark.CountdownSecondsRemaining}");
            ImGui.TextUnformatted(configuration.ShowGcd
                ? "Prepare to use GCD actions when the countdown reaches zero."
                : "Keep the cursor visible when the countdown reaches zero.");
            if (ImGui.Button("Cancel benchmark"))
            {
                benchmark.Cancel();
            }
        }
        else if (benchmark.IsCollecting)
        {
            ImGui.ProgressBar((float)benchmark.Progress, new Vector2(-1f, 0f), $"Running: {benchmark.SampleCount} frames");
            ImGui.TextUnformatted(configuration.ShowGcd
                ? "Benchmark running. Continue using GCD actions."
                : "Benchmark running. Keep the cursor visible.");
            if (configuration.ShowGcd)
            {
                ImGui.TextUnformatted(benchmark.GcdDetected ? "GCD detected: yes" : "GCD detected: not yet");
                if (configuration.ShowCastSegments)
                {
                    ImGui.TextUnformatted(benchmark.CastSegmentsDetected ? "Cast segments detected: yes" : "Cast segments detected: not yet");
                }
            }
            if (ImGui.Button("Cancel benchmark"))
            {
                benchmark.Cancel();
            }
        }
        else if (ImGui.Button("Run 10-second benchmark"))
        {
            benchmark.Start();
        }

        if (configuration.ShowGcd)
        {
            var observation = GlobalCooldownReader.LastObservation;
            ImGui.TextWrapped($"GCD reader: {GcdReadStatusLabel(observation.Status)}, native active {YesNo(observation.NativeActive)}, elapsed {observation.Elapsed:F3}, total {observation.Total:F3}");
        }

        if (benchmark.LastResult is not { } result)
        {
            return;
        }

        ImGui.TextWrapped(result.Format());
        if (ImGui.Button("Copy benchmark result"))
        {
            ImGui.SetClipboardText(result.Format());
        }
    }

    private static string GcdReadStatusLabel(GcdReadStatus value)
    {
        return value switch
        {
            GcdReadStatus.Read => "read",
            GcdReadStatus.ManagerUnavailable => "manager unavailable",
            GcdReadStatus.DetailUnavailable => "detail unavailable",
            GcdReadStatus.Failed => "failed",
            _ => "waiting"
        };
    }

    private static string YesNo(bool value)
    {
        return value ? "yes" : "no";
    }
#endif

    private void DrawPreview()
    {
        const float previewHeight = 112f;
        var start = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var center = start + new Vector2(width / 2f, previewHeight / 2f);
        var geometry = RingMath.GetGeometry(configuration);
        var ringBorder = configuration.ShowRingBorder ? configuration.RingBorderThickness : 0f;
        var dotBorder = configuration.ShowDotBorder ? configuration.DotBorderThickness : 0f;
        var gcdBorder = configuration.ShowGcd && configuration.ShowGcdBorder ? configuration.GcdBorderThickness : 0f;
        var ringExtent = geometry.Main + (configuration.RingThickness / 2f) + ringBorder;
        var dotExtent = (configuration.DotDiameter / 2f) + dotBorder;
        var gcdExtent = configuration.ShowGcd ? configuration.GcdPlacement switch
        {
            GcdPlacement.Outer => geometry.Outer + (configuration.GcdThickness / 2f) + gcdBorder,
            GcdPlacement.Inner => geometry.Inner + (geometry.InnerThickness / 2f) + geometry.InnerBorderThickness,
            GcdPlacement.Overlay when configuration.OverlayFill == OverlayFillStyle.Pie => geometry.Pie,
            _ => geometry.Main + (configuration.RingThickness / 2f) + gcdBorder
        } : 0f;
        var extent = MathF.Max(MathF.Max(ringExtent, dotExtent), gcdExtent);
        var scale = MathF.Min(1f, MathF.Min((previewHeight - 10f) / (extent * 2f), (width - 10f) / (extent * 2f)));
        var previewProgress = configuration.ProgressBehavior == ProgressBehavior.Fill ? 0.9f : 0.35f;
        var gcd = configuration.ShowGcd ? new GcdState(true, previewProgress * 2.5f, 2.5f) : GcdState.Inactive;
        var segments = configuration.ShowGcd && configuration.ShowCastSegments ? new GcdSegments(true, 0.55f, 0.78f, true) : GcdSegments.Inactive;
        CursorRenderer.DrawAt(configuration, ImGui.GetWindowDrawList(), center, gcd, segments, scale);
        ImGui.Dummy(new Vector2(width, previewHeight));
    }

    private static void DrawSection(string label, string description)
    {
        ImGui.Spacing();
        ImGui.TextUnformatted(label);
        ImGui.Separator();
        DrawHint(description);
    }

    private static void DrawHint(string text)
    {
        var disabledColor = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
        ImGui.PushStyleColor(ImGuiCol.Text, disabledColor);
        ImGui.TextWrapped(text);
        ImGui.PopStyleColor();
    }

    private static bool BeginForm(string id)
    {
        var flags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoSavedSettings;
        if (!ImGui.BeginTable($"##{id}", 2, flags))
        {
            return false;
        }

        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 168f);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
        return true;
    }

    private static bool DrawFloat(string id, string label, float value, float minimum, float maximum, string format, Action<float> setter)
    {
        BeginRow(label);
        ImGui.SetNextItemWidth(-1f);
        if (!ImGui.SliderFloat($"##{id}", ref value, minimum, maximum, format))
        {
            return false;
        }

        setter(value);
        return true;
    }

    private static bool DrawColor(string id, string label, Vector4 color, Action<Vector4> setter)
    {
        BeginRow(label);
        ImGui.SetNextItemWidth(-1f);
        if (!ImGui.ColorEdit4($"##{id}", ref color, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf))
        {
            return false;
        }

        setter(color);
        return true;
    }

    private static bool DrawEnum<T>(string id, string label, T value, Func<T, string> formatter, Action<T> setter) where T : struct, Enum
    {
        var changed = false;
        BeginRow(label);
        ImGui.SetNextItemWidth(-1f);
        if (!ImGui.BeginCombo($"##{id}", formatter(value)))
        {
            return false;
        }

        foreach (var candidate in Enum.GetValues<T>())
        {
            var selected = EqualityComparer<T>.Default.Equals(value, candidate);
            if (ImGui.Selectable(formatter(candidate), selected) && !selected)
            {
                value = candidate;
                setter(candidate);
                changed = true;
            }

            if (selected)
            {
                ImGui.SetItemDefaultFocus();
            }
        }

        ImGui.EndCombo();
        return changed;
    }

    private static void BeginRow(string label)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.TableSetColumnIndex(1);
    }

    private static string VisibilityLabel(VisibilityMode value)
    {
        return value == VisibilityMode.Always ? "Always" : "Combat only";
    }

    private static string MouseLookLabel(MouseLookVisibility value)
    {
        return value switch
        {
            MouseLookVisibility.FollowVisibility => "Same as cursor visibility",
            MouseLookVisibility.CombatOnly => "Combat only",
            _ => "Hidden"
        };
    }

    private static string GcdPlacementLabel(GcdPlacement value)
    {
        return value switch
        {
            GcdPlacement.Outer => "Outer ring",
            GcdPlacement.Inner => "Inner ring",
            _ => "Overlay"
        };
    }

    private static string OverlayFillLabel(OverlayFillStyle value)
    {
        return value == OverlayFillStyle.Stroke ? "Ring stroke" : "Filled pie";
    }

    private static string ProgressBehaviorLabel(ProgressBehavior value)
    {
        return value == ProgressBehavior.Fill ? "Fill over time" : "Drain over time";
    }

    private static string RotationLabel(RotationDirection value)
    {
        return value == RotationDirection.Clockwise ? "Clockwise" : "Counterclockwise";
    }

    private static string SlidecastTimingLabel(SlidecastTimingMode value)
    {
        return value switch
        {
            SlidecastTimingMode.Predicted => "Prediction only",
            SlidecastTimingMode.Confirmed => "Game confirmation only",
            _ => "Prediction, then confirmation"
        };
    }

    private static string SlidecastTimingDescription(SlidecastTimingMode value)
    {
        return value switch
        {
            SlidecastTimingMode.Predicted => "Uses a stable configurable estimate. The marker does not move, but it is not a guaranteed safe threshold.",
            SlidecastTimingMode.Confirmed => "Shows the slidecast segment only after the game confirms that the cast can no longer be cancelled.",
            _ => "Starts with the configurable estimate, then moves once to the game-confirmed threshold when it is observed."
        };
    }
}
