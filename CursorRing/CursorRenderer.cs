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
    private readonly CursorSettings settings;
    private readonly ICondition condition;
    private readonly IClientState clientState;
    private readonly IPlayerState playerState;
    private readonly IAddonEventManager addonEventManager;
    private readonly IUiBuilder uiBuilder;
    private readonly IPluginLog log;
    private readonly CursorPositionTracker positionTracker = new();
    private bool cursorHidden;
    private bool drawFailureLogged;

    internal CursorRenderer(
        CursorSettings settings,
        ICondition condition,
        IClientState clientState,
        IPlayerState playerState,
        IAddonEventManager addonEventManager,
        IUiBuilder uiBuilder,
        IPluginLog log)
    {
        this.settings = settings;
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
            if (!TryGetPosition(out var position))
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
            var gcd = GlobalCooldownReader.Read();
            DrawAt(settings, drawList, position, gcd);
#if CURSORRING_BENCHMARK
            if (measureGeometry)
            {
                work = new RenderWork(RenderStatus.Rendered, drawList.VtxBuffer.Size - verticesBefore, drawList.IdxBuffer.Size - indicesBefore, gcd.IsActive);
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
        SetCursorHidden(false);
    }

    internal static void DrawAt(CursorSettings settings, ImDrawListPtr drawList, Vector2 center, GcdState gcd, float scale = 1f)
    {
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
            if (settings.ShowGcdTrack)
            {
                drawList.AddCircleFilled(center, pieRadius, trackColor);
            }

            DrawPie(drawList, center, pieRadius, RingMath.GetArc(gcd.Progress, settings.ProgressBehavior, settings.Rotation), gcdColor);
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

            if (settings.ShowGcdTrack)
            {
                drawList.AddCircle(center, radius, trackColor, 0, thickness);
            }

            DrawArc(drawList, center, radius, thickness, RingMath.GetArc(gcd.Progress, settings.ProgressBehavior, settings.Rotation), gcdColor);
        }

        var dotRadius = settings.DotDiameter * scale / 2f;
        if (settings.ShowDotBorder)
        {
            var dotBorderColor = ImGui.ColorConvertFloat4ToU32(settings.DotBorderColor);
            drawList.AddCircleFilled(center, dotRadius + (settings.DotBorderThickness * scale), dotBorderColor);
        }

        drawList.AddCircleFilled(center, dotRadius, dotColor);
    }

    private unsafe bool TryGetPosition(out Vector2 position)
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
