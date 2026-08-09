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
        DrawPreview();
        ImGui.Separator();
#if CURSORRING_BENCHMARK
        DrawBenchmark();
        ImGui.Separator();
#endif

        ImGui.TextUnformatted("Visibility");
        var changed = DrawEnum("Mode", configuration.Visibility, VisibilityLabel, value => configuration.Visibility = value);
        changed |= DrawEnum("During mouse-look", configuration.MouseLook, MouseLookLabel, value => configuration.MouseLook = value);

        ImGui.Spacing();
        ImGui.TextUnformatted("Cursor");
        changed |= DrawFloat("Circle diameter", configuration.RingDiameter, 8f, 240f, "%.0f px", value => configuration.RingDiameter = value);
        changed |= DrawFloat("Circle thickness", configuration.RingThickness, 1f, 20f, "%.1f px", value => configuration.RingThickness = value);
        changed |= DrawFloat("Dot diameter", configuration.DotDiameter, 1f, 64f, "%.1f px", value => configuration.DotDiameter = value);
        changed |= DrawColor("Circle color", configuration.RingColor, value => configuration.RingColor = value);
        changed |= DrawColor("Dot color", configuration.DotColor, value => configuration.DotColor = value);
        var showRingBorder = configuration.ShowRingBorder;
        if (ImGui.Checkbox("Outline circle", ref showRingBorder))
        {
            configuration.ShowRingBorder = showRingBorder;
            changed = true;
        }
        if (configuration.ShowRingBorder)
        {
            changed |= DrawFloat("Circle outline thickness", configuration.RingBorderThickness, 1f, 20f, "%.1f px", value => configuration.RingBorderThickness = value);
            changed |= DrawColor("Circle outline color", configuration.RingBorderColor, value => configuration.RingBorderColor = value);
        }

        var showDotBorder = configuration.ShowDotBorder;
        if (ImGui.Checkbox("Outline dot", ref showDotBorder))
        {
            configuration.ShowDotBorder = showDotBorder;
            changed = true;
        }
        if (configuration.ShowDotBorder)
        {
            changed |= DrawFloat("Dot outline thickness", configuration.DotBorderThickness, 1f, 20f, "%.1f px", value => configuration.DotBorderThickness = value);
            changed |= DrawColor("Dot outline color", configuration.DotBorderColor, value => configuration.DotBorderColor = value);
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("Global cooldown");
        changed |= DrawEnum("Placement", configuration.GcdPlacement, GcdPlacementLabel, value => configuration.GcdPlacement = value);
        if (configuration.GcdPlacement == GcdPlacement.Overlay)
        {
            changed |= DrawEnum("Overlay style", configuration.OverlayFill, OverlayFillLabel, value => configuration.OverlayFill = value);
        }
        else
        {
            changed |= DrawFloat("GCD thickness", configuration.GcdThickness, 1f, 20f, "%.1f px", value => configuration.GcdThickness = value);
            changed |= DrawFloat("Ring spacing", configuration.GcdSpacing, 0f, 40f, "%.1f px", value => configuration.GcdSpacing = value);
        }

        changed |= DrawEnum("Progress", configuration.ProgressBehavior, ProgressBehaviorLabel, value => configuration.ProgressBehavior = value);
        changed |= DrawEnum("Direction", configuration.Rotation, RotationLabel, value => configuration.Rotation = value);
        changed |= DrawColor("GCD color", configuration.GcdColor, value => configuration.GcdColor = value);
        var showTrack = configuration.ShowGcdTrack;
        if (ImGui.Checkbox("Show background track", ref showTrack))
        {
            configuration.ShowGcdTrack = showTrack;
            changed = true;
        }
        if (configuration.ShowGcdTrack)
        {
            changed |= DrawColor("Track color", configuration.GcdTrackColor, value => configuration.GcdTrackColor = value);
        }

        ImGui.Spacing();
        if (ImGui.Button("Reset to defaults"))
        {
            changed |= configuration.Reset();
        }

        if (changed)
        {
            configuration.Save();
        }
    }

#if CURSORRING_BENCHMARK
    internal RenderBenchmark Benchmark => benchmark;

    private void DrawBenchmark()
    {
        ImGui.TextUnformatted("Performance benchmark");
        ImGui.TextWrapped("Measures the cursor render path for 10 seconds. Keep the ring visible for representative results.");

        if (benchmark.Phase == BenchmarkPhase.Countdown)
        {
            ImGui.ProgressBar((float)benchmark.CountdownProgress, new Vector2(-1f, 0f), $"Starting in {benchmark.CountdownSecondsRemaining}");
            ImGui.TextUnformatted("Prepare to use GCD actions when the countdown reaches zero.");
            if (ImGui.Button("Cancel benchmark"))
            {
                benchmark.Cancel();
            }
        }
        else if (benchmark.IsCollecting)
        {
            ImGui.ProgressBar((float)benchmark.Progress, new Vector2(-1f, 0f), $"Running: {benchmark.SampleCount} frames");
            ImGui.TextUnformatted("Benchmark running. Continue using GCD actions.");
            ImGui.TextUnformatted(benchmark.GcdDetected ? "GCD detected: yes" : "GCD detected: not yet");
            if (ImGui.Button("Cancel benchmark"))
            {
                benchmark.Cancel();
            }
        }
        else if (ImGui.Button("Run 10-second benchmark"))
        {
            benchmark.Start();
        }

        var observation = GlobalCooldownReader.LastObservation;
        ImGui.TextUnformatted($"GCD reader: {GcdReadStatusLabel(observation.Status)}, native active {YesNo(observation.NativeActive)}, elapsed {observation.Elapsed:F3}, total {observation.Total:F3}");

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
        const float previewHeight = 130f;
        var start = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var center = start + new Vector2(width / 2f, previewHeight / 2f);
        var geometry = RingMath.GetGeometry(configuration);
        var ringBorder = configuration.ShowRingBorder ? configuration.RingBorderThickness : 0f;
        var dotBorder = configuration.ShowDotBorder ? configuration.DotBorderThickness : 0f;
        var ringExtent = geometry.Main + (configuration.RingThickness / 2f) + ringBorder;
        var dotExtent = (configuration.DotDiameter / 2f) + dotBorder;
        var gcdExtent = geometry.Outer + (configuration.GcdThickness / 2f);
        var extent = MathF.Max(MathF.Max(ringExtent, dotExtent), gcdExtent);
        var scale = MathF.Min(1f, MathF.Min((previewHeight - 10f) / (extent * 2f), (width - 10f) / (extent * 2f)));
        CursorRenderer.DrawAt(configuration, ImGui.GetWindowDrawList(), center, new GcdState(true, 0.35f), scale);
        ImGui.Dummy(new Vector2(width, previewHeight));
    }

    private static bool DrawFloat(string label, float value, float minimum, float maximum, string format, Action<float> setter)
    {
        ImGui.SetNextItemWidth(-1f);
        if (!ImGui.SliderFloat(label, ref value, minimum, maximum, format))
        {
            return false;
        }

        setter(value);
        return true;
    }

    private static bool DrawColor(string label, Vector4 color, Action<Vector4> setter)
    {
        if (!ImGui.ColorEdit4(label, ref color, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf))
        {
            return false;
        }

        setter(color);
        return true;
    }

    private static bool DrawEnum<T>(string label, T value, Func<T, string> formatter, Action<T> setter) where T : struct, Enum
    {
        var changed = false;
        ImGui.SetNextItemWidth(-1f);
        if (!ImGui.BeginCombo(label, formatter(value)))
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

    private static string VisibilityLabel(VisibilityMode value)
    {
        return value == VisibilityMode.Always ? "Always" : "Combat only";
    }

    private static string MouseLookLabel(MouseLookVisibility value)
    {
        return value switch
        {
            MouseLookVisibility.FollowVisibility => "Follow visibility mode",
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
        return value == OverlayFillStyle.Stroke ? "Circle stroke" : "Interior pie";
    }

    private static string ProgressBehaviorLabel(ProgressBehavior value)
    {
        return value == ProgressBehavior.Fill ? "Fill from empty" : "Drain from full";
    }

    private static string RotationLabel(RotationDirection value)
    {
        return value == RotationDirection.Clockwise ? "Clockwise" : "Counterclockwise";
    }
}
