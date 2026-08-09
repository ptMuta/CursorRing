using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace CursorRing;

internal sealed class CursorRenderer
{
    private const MouseButtonFlags MouseLookButtons = MouseButtonFlags.LBUTTON | MouseButtonFlags.RBUTTON;
    private readonly ProfileManager profiles;
    private readonly ICondition condition;
    private readonly IClientState clientState;
    private readonly IPlayerState playerState;
    private readonly IAddonEventManager addonEventManager;
    private readonly IUiBuilder uiBuilder;
    private readonly IPluginLog log;
    private readonly CursorPositionTracker positionTracker = new();
    private readonly CastSegmentationTracker castSegmentationTracker = new();
    private bool cursorHidden;
    private bool drawFailureLogged;
    private bool castSegmentationEnabled;

    internal CursorRenderer(
        ProfileManager profiles,
        ICondition condition,
        IClientState clientState,
        IPlayerState playerState,
        IAddonEventManager addonEventManager,
        IUiBuilder uiBuilder,
        IPluginLog log)
    {
        this.profiles = profiles;
        this.condition = condition;
        this.clientState = clientState;
        this.playerState = playerState;
        this.addonEventManager = addonEventManager;
        this.uiBuilder = uiBuilder;
        this.log = log;
    }

    internal void Draw()
    {
#if CURSORRING_BENCHMARK
        DrawCore(false, out _);
#else
        DrawCore();
#endif
    }

#if CURSORRING_BENCHMARK
    internal RenderWork DrawMeasured()
    {
        DrawCore(true, out var work);
        return work;
    }
#endif

#if CURSORRING_BENCHMARK
    private bool DrawCore(bool measureGeometry, out RenderWork work)
#else
    private bool DrawCore()
#endif
    {
#if CURSORRING_BENCHMARK
        work = default;
#endif
        try
        {
            var settings = profiles.ActiveSettings;
            if (!TryGetPosition(settings, out var position))
            {
                Hide();
                return false;
            }

            SetCursorHidden(true);
            var drawList = ImGui.GetForegroundDrawList(ImGui.GetMainViewport());
#if CURSORRING_BENCHMARK
            var verticesBefore = measureGeometry ? drawList.VtxBuffer.Size : 0;
            var indicesBefore = measureGeometry ? drawList.IdxBuffer.Size : 0;
#endif
            var gcd = settings.ShowGcd ? GlobalCooldownReader.Read() : GcdState.Inactive;
            var segments = GcdSegments.Inactive;
            if (settings.ShowGcd && settings.ShowCastSegments)
            {
                castSegmentationEnabled = true;
                var cast = gcd.IsActive ? LocalCastReader.Read(castSegmentationTracker.NeedsCast(gcd)) : CastSample.Inactive;
                segments = castSegmentationTracker.Update(gcd, cast, settings.SlidecastTiming, settings.SlidecastPredictionMilliseconds);
            }
            else if (castSegmentationEnabled)
            {
                castSegmentationEnabled = false;
                castSegmentationTracker.Reset();
            }

            DrawAt(settings, drawList, position, gcd, segments);
#if CURSORRING_BENCHMARK
            if (measureGeometry)
            {
                work = new RenderWork(RenderStatus.Rendered, drawList.VtxBuffer.Size - verticesBefore, drawList.IdxBuffer.Size - indicesBefore, gcd.IsActive, segments.IsActive);
            }
#endif
            drawFailureLogged = false;
            return true;
        }
        catch (Exception exception)
        {
#if CURSORRING_BENCHMARK
            if (measureGeometry)
            {
                work = new RenderWork(RenderStatus.Failed, 0, 0, false);
            }
#endif
            Hide();
            if (!drawFailureLogged)
            {
                log.Error(exception, "CursorRing rendering failed.");
                drawFailureLogged = true;
            }

            return false;
        }
    }

    internal void Hide()
    {
        if (cursorHidden && castSegmentationEnabled)
        {
            castSegmentationTracker.Reset();
        }

        SetCursorHidden(false);
    }

    internal void ResetState()
    {
        castSegmentationEnabled = false;
        castSegmentationTracker.Reset();
        Hide();
    }

    internal static void DrawAt(
        CursorSettings settings,
        ImDrawListPtr drawList,
        Vector2 center,
        GcdState gcd,
        GcdSegments segments,
        float scale = 1f)
    {
        if (!settings.ShowGcd)
        {
            gcd = GcdState.Inactive;
            segments = GcdSegments.Inactive;
        }

        var geometry = RingMath.GetGeometry(settings);
        var mainRadius = geometry.Main * scale;
        var innerRadius = geometry.Inner * scale;
        var outerRadius = geometry.Outer * scale;
        var pieRadius = geometry.Pie * scale;
        var ringThickness = settings.RingThickness * scale;
        var gcdThickness = settings.GcdThickness * scale;
        var innerThickness = geometry.InnerThickness * scale;
        var ringColor = ImGui.ColorConvertFloat4ToU32(settings.RingColor);
        var dotColor = ImGui.ColorConvertFloat4ToU32(settings.DotColor);
        var gcdColor = ImGui.ColorConvertFloat4ToU32(settings.GcdColor);
        var trackColor = ImGui.ColorConvertFloat4ToU32(settings.GcdTrackColor);

        if (gcd.IsActive && settings.GcdPlacement == GcdPlacement.Overlay && settings.OverlayFill == OverlayFillStyle.Pie)
        {
            DrawGcdPie(settings, drawList, center, pieRadius, gcd, segments, scale, gcdColor, trackColor);
        }

        if (settings.ShowRingBorder)
        {
            var ringBorderColor = ImGui.ColorConvertFloat4ToU32(settings.RingBorderColor);
            drawList.AddCircle(center, mainRadius, ringBorderColor, 0, ringThickness + (settings.RingBorderThickness * scale * 2f));
        }

        drawList.AddCircle(center, mainRadius, ringColor, 0, ringThickness);

        if (gcd.IsActive && (settings.GcdPlacement != GcdPlacement.Overlay || settings.OverlayFill == OverlayFillStyle.Stroke))
        {
            var radius = settings.GcdPlacement switch
            {
                GcdPlacement.Inner => innerRadius,
                GcdPlacement.Outer => outerRadius,
                _ => mainRadius
            };
            var thickness = settings.GcdPlacement switch
            {
                GcdPlacement.Inner => innerThickness,
                GcdPlacement.Outer => gcdThickness,
                _ => ringThickness
            };
            var borderThickness = settings.GcdPlacement switch
            {
                GcdPlacement.Inner => geometry.InnerBorderThickness * scale,
                GcdPlacement.Overlay => RingMath.ClampStrokeBorder(mainRadius, ringThickness, settings.GcdBorderThickness * scale),
                _ => settings.GcdBorderThickness * scale
            };

            DrawGcdStroke(settings, drawList, center, radius, thickness, borderThickness, gcd, segments, scale, gcdColor, trackColor);
        }

        var dotRadius = settings.DotDiameter * scale / 2f;
        if (settings.ShowDotBorder)
        {
            var dotBorderColor = ImGui.ColorConvertFloat4ToU32(settings.DotBorderColor);
            drawList.AddCircleFilled(center, dotRadius + (settings.DotBorderThickness * scale), dotBorderColor);
        }

        drawList.AddCircleFilled(center, dotRadius, dotColor);
    }

    private unsafe bool TryGetPosition(CursorSettings settings, out Vector2 position)
    {
        position = default;
        if (!clientState.IsLoggedIn || !playerState.IsLoaded || !uiBuilder.ShouldModifyUi || ImGui.GetIO().AppFocusLost)
        {
            return false;
        }

        var nativeCursor = Cursor.Instance();
        if (nativeCursor is null)
        {
            return false;
        }

        var mouseLook = IsMouseLookActive();
        if (!mouseLook && nativeCursor->IsCursorOutsideViewPort)
        {
            return false;
        }

        var inCombat = condition[ConditionFlag.InCombat];
        var visible = settings.Visibility == VisibilityMode.Always || inCombat;
        if (mouseLook)
        {
            visible = settings.MouseLook switch
            {
                MouseLookVisibility.FollowVisibility => visible,
                MouseLookVisibility.CombatOnly => inCombat,
                _ => false
            };
        }

        if (!visible)
        {
            return false;
        }

        var viewport = ImGui.GetMainViewport();
        var mouse = ImGui.GetMousePos();
        var maximum = viewport.Pos + viewport.Size;
        return positionTracker.TryResolve(mouse, viewport.Pos, maximum, mouseLook, out position);
    }

    private static unsafe bool IsMouseLookActive()
    {
        var input = UIInputData.Instance();
        return input is not null
            && (input->UIFilteredCursorInputs.MouseButtonHeldFlags & MouseLookButtons) != 0;
    }

    private void SetCursorHidden(bool hidden)
    {
        if (cursorHidden == hidden)
        {
            return;
        }

        if (hidden)
        {
            addonEventManager.SetCursor(AddonCursorType.Hidden);
        }
        else
        {
            addonEventManager.ResetCursor();
        }

        cursorHidden = hidden;
    }

    private static void DrawGcdStroke(
        CursorSettings settings,
        ImDrawListPtr drawList,
        Vector2 center,
        float radius,
        float thickness,
        float borderThickness,
        GcdState gcd,
        GcdSegments segments,
        float scale,
        uint gcdColor,
        uint trackColor)
    {
        var visible = RingMath.GetVisibleRange(gcd.Progress, settings.ProgressBehavior);
        if (settings.ShowGcdBorder && borderThickness > 0f)
        {
            var borderColor = ImGui.ColorConvertFloat4ToU32(settings.GcdBorderColor);
            if (settings.ShowGcdTrack)
            {
                drawList.AddCircle(center, radius, borderColor, 0, thickness + (borderThickness * 2f));
            }
            else
            {
                DrawArcRange(drawList, center, radius, thickness + (borderThickness * 2f), visible, settings.Rotation, borderColor);
            }
        }

        if (settings.ShowGcdTrack)
        {
            drawList.AddCircle(center, radius, trackColor, 0, thickness);
        }

        DrawGcdArcContent(settings, drawList, center, radius, thickness, visible, segments, gcdColor);
        DrawSegmentDividers(settings, drawList, center, radius, thickness, visible, segments, scale, false);
    }

    private static void DrawGcdPie(
        CursorSettings settings,
        ImDrawListPtr drawList,
        Vector2 center,
        float radius,
        GcdState gcd,
        GcdSegments segments,
        float scale,
        uint gcdColor,
        uint trackColor)
    {
        var visible = RingMath.GetVisibleRange(gcd.Progress, settings.ProgressBehavior);
        var borderThickness = settings.ShowGcdBorder
            ? RingMath.ClampPieBorder(radius, settings.GcdBorderThickness * scale)
            : 0f;
        var contentRadius = MathF.Max(0.5f, radius - borderThickness);
        if (settings.ShowGcdBorder)
        {
            var borderColor = ImGui.ColorConvertFloat4ToU32(settings.GcdBorderColor);
            if (settings.ShowGcdTrack)
            {
                drawList.AddCircleFilled(center, radius, borderColor);
            }
            else
            {
                DrawPieRange(drawList, center, radius, visible, settings.Rotation, borderColor);
            }
        }

        if (settings.ShowGcdTrack)
        {
            drawList.AddCircleFilled(center, contentRadius, trackColor);
        }

        DrawGcdPieContent(settings, drawList, center, contentRadius, visible, segments, gcdColor);
        if (settings.ShowGcdBorder && !settings.ShowGcdTrack && visible.IsVisible)
        {
            var borderColor = ImGui.ColorConvertFloat4ToU32(settings.GcdBorderColor);
            DrawPieEdges(drawList, center, radius, visible, settings.Rotation, borderThickness, borderColor);
        }

        DrawSegmentDividers(settings, drawList, center, contentRadius, contentRadius, visible, segments, scale, true);
    }

    private static void DrawGcdArcContent(
        CursorSettings settings,
        ImDrawListPtr drawList,
        Vector2 center,
        float radius,
        float thickness,
        ProgressRange visible,
        GcdSegments segments,
        uint gcdColor)
    {
        if (!segments.IsActive)
        {
            DrawArcRange(drawList, center, radius, thickness, visible, settings.Rotation, gcdColor);
            return;
        }

        var castColor = ImGui.ColorConvertFloat4ToU32(settings.CastSegmentColor);
        var slidecastColor = ImGui.ColorConvertFloat4ToU32(settings.SlidecastSegmentColor);
        DrawArcRange(drawList, center, radius, thickness, RingMath.Intersect(visible, new ProgressRange(0f, segments.SlideStart)), settings.Rotation, castColor);
        DrawArcRange(drawList, center, radius, thickness, RingMath.Intersect(visible, new ProgressRange(segments.SlideStart, segments.CastEnd)), settings.Rotation, slidecastColor);
        DrawArcRange(drawList, center, radius, thickness, RingMath.Intersect(visible, new ProgressRange(segments.CastEnd, 1f)), settings.Rotation, gcdColor);
    }

    private static void DrawGcdPieContent(
        CursorSettings settings,
        ImDrawListPtr drawList,
        Vector2 center,
        float radius,
        ProgressRange visible,
        GcdSegments segments,
        uint gcdColor)
    {
        if (!segments.IsActive)
        {
            DrawPieRange(drawList, center, radius, visible, settings.Rotation, gcdColor);
            return;
        }

        var castColor = ImGui.ColorConvertFloat4ToU32(settings.CastSegmentColor);
        var slidecastColor = ImGui.ColorConvertFloat4ToU32(settings.SlidecastSegmentColor);
        DrawPieRange(drawList, center, radius, RingMath.Intersect(visible, new ProgressRange(0f, segments.SlideStart)), settings.Rotation, castColor);
        DrawPieRange(drawList, center, radius, RingMath.Intersect(visible, new ProgressRange(segments.SlideStart, segments.CastEnd)), settings.Rotation, slidecastColor);
        DrawPieRange(drawList, center, radius, RingMath.Intersect(visible, new ProgressRange(segments.CastEnd, 1f)), settings.Rotation, gcdColor);
    }

    private static void DrawSegmentDividers(
        CursorSettings settings,
        ImDrawListPtr drawList,
        Vector2 center,
        float radius,
        float visualWidth,
        ProgressRange visible,
        GcdSegments segments,
        float scale,
        bool pie)
    {
        if (!segments.IsActive || !settings.ShowSegmentDividers)
        {
            return;
        }

        var color = ImGui.ColorConvertFloat4ToU32(settings.SegmentDividerColor);
        var thickness = settings.SegmentDividerThickness * scale;
        DrawSegmentDivider(settings, drawList, center, radius, visualWidth, visible, segments.SlideStart, pie, color, thickness);
        if (MathF.Abs(segments.CastEnd - segments.SlideStart) > 0.0001f)
        {
            DrawSegmentDivider(settings, drawList, center, radius, visualWidth, visible, segments.CastEnd, pie, color, thickness);
        }
    }

    private static void DrawSegmentDivider(
        CursorSettings settings,
        ImDrawListPtr drawList,
        Vector2 center,
        float radius,
        float visualWidth,
        ProgressRange visible,
        float progress,
        bool pie,
        uint color,
        float thickness)
    {
        if (progress <= 0.0001f
            || progress >= 0.9999f
            || !settings.ShowGcdTrack && (progress < visible.Start || progress > visible.End))
        {
            return;
        }

        var direction = settings.Rotation == RotationDirection.Clockwise ? 1f : -1f;
        var angle = RingMath.Top + (direction * RingMath.FullTurn * progress);
        var startRadius = pie ? 0f : MathF.Max(0f, radius - (visualWidth / 2f));
        var endRadius = pie ? radius : radius + (visualWidth / 2f);
        drawList.AddLine(PointOnCircle(center, startRadius, angle), PointOnCircle(center, endRadius, angle), color, thickness);
    }

    private static void DrawArcRange(
        ImDrawListPtr drawList,
        Vector2 center,
        float radius,
        float thickness,
        ProgressRange range,
        RotationDirection direction,
        uint color)
    {
        if (range.IsVisible)
        {
            DrawArc(drawList, center, radius, thickness, RingMath.GetArc(range.Start, range.End, direction), color);
        }
    }

    private static void DrawPieRange(
        ImDrawListPtr drawList,
        Vector2 center,
        float radius,
        ProgressRange range,
        RotationDirection direction,
        uint color)
    {
        if (range.IsVisible)
        {
            DrawPie(drawList, center, radius, RingMath.GetArc(range.Start, range.End, direction), color);
        }
    }

    private static void DrawPieEdges(
        ImDrawListPtr drawList,
        Vector2 center,
        float radius,
        ProgressRange range,
        RotationDirection direction,
        float thickness,
        uint color)
    {
        if (range.End - range.Start >= 0.9999f)
        {
            drawList.AddCircle(center, MathF.Max(0.5f, radius - (thickness / 2f)), color, 0, thickness);
            return;
        }

        var sign = direction == RotationDirection.Clockwise ? 1f : -1f;
        var startAngle = RingMath.Top + (sign * RingMath.FullTurn * range.Start);
        var endAngle = RingMath.Top + (sign * RingMath.FullTurn * range.End);
        drawList.AddLine(center, PointOnCircle(center, radius, startAngle), color, thickness);
        drawList.AddLine(center, PointOnCircle(center, radius, endAngle), color, thickness);
        DrawArc(drawList, center, MathF.Max(0.5f, radius - (thickness / 2f)), thickness, new ArcAngles(startAngle, endAngle), color);
    }

    private static void DrawArc(ImDrawListPtr drawList, Vector2 center, float radius, float thickness, ArcAngles angles, uint color)
    {
        var sweep = angles.End - angles.Start;
        if (MathF.Abs(sweep) <= 0.0001f)
        {
            return;
        }

        if (MathF.Abs(sweep) >= RingMath.FullTurn - 0.0001f)
        {
            drawList.AddCircle(center, radius, color, 0, thickness);
            return;
        }

        drawList.PathArcTo(center, radius, angles.Start, angles.End);
        drawList.PathStroke(color, ImDrawFlags.None, thickness);
    }

    private static void DrawPie(ImDrawListPtr drawList, Vector2 center, float radius, ArcAngles angles, uint color)
    {
        var sweep = angles.End - angles.Start;
        if (MathF.Abs(sweep) <= 0.0001f)
        {
            return;
        }

        if (MathF.Abs(sweep) >= RingMath.FullTurn - 0.0001f)
        {
            drawList.AddCircleFilled(center, radius, color);
            return;
        }

        var segments = Math.Clamp((int)MathF.Ceiling(MathF.Abs(sweep) * MathF.Sqrt(radius)), 4, 128);
        var previous = PointOnCircle(center, radius, angles.Start);
        for (var index = 1; index <= segments; index++)
        {
            var angle = angles.Start + (sweep * index / segments);
            var next = PointOnCircle(center, radius, angle);
            drawList.AddTriangleFilled(center, previous, next, color);
            previous = next;
        }
    }

    private static Vector2 PointOnCircle(Vector2 center, float radius, float angle)
    {
        return center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
    }
}
