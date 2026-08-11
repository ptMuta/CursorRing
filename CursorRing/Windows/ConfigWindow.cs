using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;

namespace CursorRing.Windows;

internal sealed class ConfigWindow : Window
{
    private enum ProfileDomain
    {
        Cursor,
        Gcd,
        CastTiming
    }

    private static readonly AssignmentScope[] AssignmentScopes = [AssignmentScope.Territory, AssignmentScope.Duty, AssignmentScope.PvP];
    private readonly Configuration configuration;
    private readonly ProfileManager profiles;
    private readonly InstanceCatalog catalog;
    private Guid selectedProfileId;
    private string newProfileName = string.Empty;
    private Guid newProfileSourceId;
    private string renameProfileName = string.Empty;
    private AssignmentScope draftScope;
    private uint draftTargetId;
    private uint draftZoneId;
    private uint draftDutyId;
    private uint draftPvpId;
    private Guid draftProfileId;
    private string locationSearch = string.Empty;
    private bool assignmentDraftInitialized;
    private ProfileDomain profileDomain;
#if CURSORRING_BENCHMARK
    private readonly RenderBenchmark benchmark;
#endif

    internal ConfigWindow(Configuration configuration, ProfileManager profiles, InstanceCatalog catalog)
        : base("CursorRing Settings###CursorRingConfig", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.configuration = configuration;
        this.profiles = profiles;
        this.catalog = catalog;
#if CURSORRING_BENCHMARK
        benchmark = new RenderBenchmark();
#endif
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(720f, 640f),
            MaximumSize = new Vector2(1000f, 900f)
        };
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("profile_tabs"))
        {
            if (ImGui.BeginTabItem("Profiles"))
            {
                DrawProfileSelector();
                DrawProfileEditor();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Assignments"))
            {
                DrawAssignments();
                ImGui.EndTabItem();
            }
#if CURSORRING_BENCHMARK
            if (ImGui.BeginTabItem("Benchmark"))
            {
                DrawSection("Performance benchmark", "Available only in Debug and Benchmark builds.");
                DrawBenchmark();
                ImGui.EndTabItem();
            }
#endif
            ImGui.EndTabBar();
        }
    }

    public override void OnOpen()
    {
        selectedProfileId = configuration.DefaultProfileId;
        assignmentDraftInitialized = false;
    }

    private CursorSettings Settings
    {
        get
        {
            for (var index = 0; index < configuration.Profiles.Count; index++)
            {
                if (configuration.Profiles[index].Id == selectedProfileId)
                {
                    return configuration.Profiles[index].Settings;
                }
            }
            return configuration;
        }
    }

    private void DrawProfileEditor()
    {
        var height = Math.Max(1f, ImGui.GetContentRegionAvail().Y);
        var flags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings;
        if (!ImGui.BeginTable("profile_editor", 2, flags))
        {
            return;
        }
        ImGui.TableSetupColumn("Preview", ImGuiTableColumnFlags.WidthStretch, 0.7f);
        ImGui.TableSetupColumn("Settings", ImGuiTableColumnFlags.WidthStretch, 1.3f);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);
        ImGui.BeginChild("profile_preview", new Vector2(0f, height), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        DrawPreview();
        ImGui.EndChild();
        ImGui.PopStyleColor();
        ImGui.TableSetColumnIndex(1);
        DrawProfileSettings();
        ImGui.EndTable();
    }

    private void DrawProfileSettings()
    {
        if (ImGui.BeginTabBar("profile_domains"))
        {
            if (ImGui.BeginTabItem("Cursor"))
            {
                profileDomain = ProfileDomain.Cursor;
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("GCD"))
            {
                profileDomain = ProfileDomain.Gcd;
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Cast timing"))
            {
                profileDomain = ProfileDomain.CastTiming;
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
        ImGui.BeginChild("profile_settings", new Vector2(0f, Math.Max(1f, ImGui.GetContentRegionAvail().Y)), false);
        switch (profileDomain)
        {
            case ProfileDomain.Gcd:
                DrawGcdSettings();
                break;
            case ProfileDomain.CastTiming:
                DrawCastSettings();
                break;
            default:
                DrawCursorSettings();
                break;
        }
        ImGui.EndChild();
    }

    private void DrawCursorSettings()
    {
        DrawSection("Visibility", "Choose when CursorRing replaces the game cursor.");
        var changed = false;
        if (BeginForm("visibility"))
        {
            changed |= DrawEnum("visibility_mode", "Cursor visibility", Settings.Visibility, VisibilityLabel, value => Settings.Visibility = value);
            changed |= DrawEnum("mouse_look", "Mouse-look visibility", Settings.MouseLook, MouseLookLabel, value => Settings.MouseLook = value);
            ImGui.EndTable();
        }

        DrawSection("Interactable hover", "React when a targetable world entity or its nameplate is under the cursor.");
        var showHoverIndicator = Settings.ShowHoverIndicator;
        if (ImGui.Checkbox("Hover indicator", ref showHoverIndicator))
        {
            Settings.ShowHoverIndicator = showHoverIndicator;
            changed = true;
        }
        if (Settings.ShowHoverIndicator)
        {
            if (BeginForm("hover_indicator"))
            {
                changed |= DrawEnum("hover_visibility", "Visibility", Settings.HoverVisibility, HoverVisibilityLabel, value => Settings.HoverVisibility = value);
                changed |= DrawEnum("hover_style", "Style", Settings.HoverIndicatorStyle, HoverIndicatorStyleLabel, value => Settings.HoverIndicatorStyle = value);
                changed |= DrawFloat("hover_size", "Element size", Settings.HoverIndicatorSize, 2f, 32f, "%.1f px", value => Settings.HoverIndicatorSize = value);
                changed |= DrawFloat("hover_thickness", "Stroke thickness", Settings.HoverIndicatorThickness, 1f, MathF.Min(8f, Settings.HoverIndicatorSize), "%.1f px", value => Settings.HoverIndicatorThickness = value);
                changed |= DrawFloat("hover_offset", "Distance from dot", Settings.HoverIndicatorOffset, 0f, 40f, "%.1f px", value => Settings.HoverIndicatorOffset = value);
                changed |= DrawFloat("hover_rotation", "Rotation", Settings.HoverIndicatorRotationDegrees, -180f, 180f, "%.0f°", value => Settings.HoverIndicatorRotationDegrees = value);
                changed |= DrawColor("hover_indicator_color", "Indicator color", Settings.HoverIndicatorColor, value => Settings.HoverIndicatorColor = value);
                ImGui.EndTable();
            }

            var useHoverRingColor = Settings.UseHoverRingColor;
            if (ImGui.Checkbox("Change ring color on hover", ref useHoverRingColor))
            {
                Settings.UseHoverRingColor = useHoverRingColor;
                changed = true;
            }
            if (Settings.UseHoverRingColor)
            {
                ImGui.Indent();
                if (BeginForm("hover_ring_color"))
                {
                    changed |= DrawColor("hover_ring_color_value", "Color", Settings.HoverRingColor, value => Settings.HoverRingColor = value);
                    ImGui.EndTable();
                }
                ImGui.Unindent();
            }

            var hideDotOnHover = Settings.HideDotOnHover;
            if (ImGui.Checkbox("Hide dot on hover", ref hideDotOnHover))
            {
                Settings.HideDotOnHover = hideDotOnHover;
                changed = true;
            }
            if (!Settings.HideDotOnHover)
            {
                var useHoverDotColor = Settings.UseHoverDotColor;
                if (ImGui.Checkbox("Change dot color on hover", ref useHoverDotColor))
                {
                    Settings.UseHoverDotColor = useHoverDotColor;
                    changed = true;
                }
                if (Settings.UseHoverDotColor)
                {
                    ImGui.Indent();
                    if (BeginForm("hover_dot_color"))
                    {
                        changed |= DrawColor("hover_dot_color_value", "Color", Settings.HoverDotColor, value => Settings.HoverDotColor = value);
                        ImGui.EndTable();
                    }
                    ImGui.Unindent();
                }
            }
        }

        DrawSection("Cursor appearance", "Sizes are measured in screen pixels.");
        if (BeginForm("cursor"))
        {
            changed |= DrawFloat("circle_diameter", "Ring diameter", Settings.RingDiameter, 8f, 240f, "%.0f px", value => Settings.RingDiameter = value);
            changed |= DrawFloat("circle_thickness", "Ring thickness", Settings.RingThickness, 1f, 20f, "%.1f px", value => Settings.RingThickness = value);
            changed |= DrawColor("circle_color", "Ring color", Settings.RingColor, value => Settings.RingColor = value);
            changed |= DrawFloat("dot_diameter", "Dot diameter", Settings.DotDiameter, 1f, 64f, "%.1f px", value => Settings.DotDiameter = value);
            changed |= DrawColor("dot_color", "Dot color", Settings.DotColor, value => Settings.DotColor = value);
            ImGui.EndTable();
        }

        var showRingBorder = Settings.ShowRingBorder;
        if (ImGui.Checkbox("Ring outline", ref showRingBorder))
        {
            Settings.ShowRingBorder = showRingBorder;
            changed = true;
        }
        if (Settings.ShowRingBorder)
        {
            ImGui.Indent();
            if (BeginForm("circle_outline"))
            {
                changed |= DrawFloat("circle_outline_thickness", "Thickness", Settings.RingBorderThickness, 1f, 20f, "%.1f px", value => Settings.RingBorderThickness = value);
                changed |= DrawColor("circle_outline_color", "Color", Settings.RingBorderColor, value => Settings.RingBorderColor = value);
                ImGui.EndTable();
            }
            ImGui.Unindent();
        }

        var showDotBorder = Settings.ShowDotBorder;
        if (ImGui.Checkbox("Dot outline", ref showDotBorder))
        {
            Settings.ShowDotBorder = showDotBorder;
            changed = true;
        }
        if (Settings.ShowDotBorder)
        {
            ImGui.Indent();
            if (BeginForm("dot_outline"))
            {
                changed |= DrawFloat("dot_outline_thickness", "Thickness", Settings.DotBorderThickness, 1f, 20f, "%.1f px", value => Settings.DotBorderThickness = value);
                changed |= DrawColor("dot_outline_color", "Color", Settings.DotBorderColor, value => Settings.DotBorderColor = value);
                ImGui.EndTable();
            }
            ImGui.Unindent();
        }

        if (changed)
        {
            configuration.Save();
        }
    }

    private void DrawGcdSettings()
    {
        var changed = false;
        DrawSection("Global cooldown", "Shown only while the global cooldown is active.");
        var showGcd = Settings.ShowGcd;
        if (ImGui.Checkbox("GCD indicator", ref showGcd))
        {
            Settings.ShowGcd = showGcd;
            changed = true;
        }
        if (Settings.ShowGcd)
        {
            if (BeginForm("gcd"))
            {
                changed |= DrawEnum("gcd_placement", "Placement", Settings.GcdPlacement, GcdPlacementLabel, value => Settings.GcdPlacement = value);
                if (Settings.GcdPlacement == GcdPlacement.Overlay)
                {
                    changed |= DrawEnum("overlay_style", "Overlay style", Settings.OverlayFill, OverlayFillLabel, value => Settings.OverlayFill = value);
                }
                else
                {
                    changed |= DrawFloat("gcd_thickness", "Ring thickness", Settings.GcdThickness, 1f, 20f, "%.1f px", value => Settings.GcdThickness = value);
                    changed |= DrawFloat("gcd_spacing", "Gap from cursor ring", Settings.GcdSpacing, 0f, 40f, "%.1f px", value => Settings.GcdSpacing = value);
                }

                changed |= DrawEnum("progress_behavior", "Progress behavior", Settings.ProgressBehavior, ProgressBehaviorLabel, value => Settings.ProgressBehavior = value);
                changed |= DrawEnum("rotation_direction", "Rotation direction", Settings.Rotation, RotationLabel, value => Settings.Rotation = value);
                changed |= DrawColor("gcd_color", "GCD / post-cast color", Settings.GcdColor, value => Settings.GcdColor = value);
                ImGui.EndTable();
            }

            var showTrack = Settings.ShowGcdTrack;
            if (ImGui.Checkbox("Background track", ref showTrack))
            {
                Settings.ShowGcdTrack = showTrack;
                changed = true;
            }
            if (Settings.ShowGcdTrack)
            {
                ImGui.Indent();
                if (BeginForm("gcd_track"))
                {
                    changed |= DrawColor("track_color", "Color", Settings.GcdTrackColor, value => Settings.GcdTrackColor = value);
                    ImGui.EndTable();
                }
                ImGui.Unindent();
            }

            var showGcdBorder = Settings.ShowGcdBorder;
            if (ImGui.Checkbox("GCD outline", ref showGcdBorder))
            {
                Settings.ShowGcdBorder = showGcdBorder;
                changed = true;
            }
            if (Settings.ShowGcdBorder)
            {
                ImGui.Indent();
                if (BeginForm("gcd_outline"))
                {
                    changed |= DrawFloat("gcd_outline_thickness", "Thickness", Settings.GcdBorderThickness, 1f, 20f, "%.1f px", value => Settings.GcdBorderThickness = value);
                    changed |= DrawColor("gcd_outline_color", "Color", Settings.GcdBorderColor, value => Settings.GcdBorderColor = value);
                    ImGui.EndTable();
                }
                ImGui.Unindent();
            }
        }

        if (changed)
        {
            configuration.Save();
        }
    }

    private void DrawCastSettings()
    {
        DrawSection("Cast timing", "Show cast, slidecast, and post-cast segments. Long casts scale the ring to their complete live duration.");
        if (!Settings.ShowGcd)
        {
            DrawHint("Enable the GCD indicator before configuring cast timing.");
            return;
        }
        var changed = false;
        var showCastSegments = Settings.ShowCastSegments;
        if (ImGui.Checkbox("Cast timing segments", ref showCastSegments))
        {
            Settings.ShowCastSegments = showCastSegments;
            changed = true;
        }
        if (Settings.ShowCastSegments)
        {
            if (BeginForm("cast_timing"))
            {
                changed |= DrawFloat("predicted_grace", "Predicted grace window", Settings.SlidecastPredictionMilliseconds, 0f, 1000f, "%.0f ms", value => Settings.SlidecastPredictionMilliseconds = value);
                DrawHint("Prediction estimates the window from the live cast total. Confirmation uses the matching response after it is observed.");
                changed |= DrawColor("casting_color", "Casting color", Settings.CastSegmentColor, value => Settings.CastSegmentColor = value);
                changed |= DrawColor("slidecast_color", "Slidecast color", Settings.SlidecastSegmentColor, value => Settings.SlidecastSegmentColor = value);
                ImGui.EndTable();
            }

            var showDividers = Settings.ShowSegmentDividers;
            if (ImGui.Checkbox("Segment dividers", ref showDividers))
            {
                Settings.ShowSegmentDividers = showDividers;
                changed = true;
            }
            if (Settings.ShowSegmentDividers)
            {
                ImGui.Indent();
                if (BeginForm("segment_dividers"))
                {
                    changed |= DrawFloat("divider_thickness", "Thickness", Settings.SegmentDividerThickness, 1f, 10f, "%.1f px", value => Settings.SegmentDividerThickness = value);
                    changed |= DrawColor("divider_color", "Color", Settings.SegmentDividerColor, value => Settings.SegmentDividerColor = value);
                    ImGui.EndTable();
                }
                ImGui.Unindent();
            }
        }

        if (changed)
        {
            configuration.Save();
        }
    }

    private void DrawProfileSelector()
    {
        var openNewProfile = false;
        var openDuplicateProfile = false;
        var openRenameProfile = false;
        var setDefaultProfile = false;
        var openResetProfile = false;
        var openDeleteProfile = false;
        var formFlags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings;
        if (ImGui.BeginTable("profile_picker", 2, formFlags))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 70f);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Profile");
            ImGui.TableSetColumnIndex(1);
            var buttonSize = ImGui.GetFrameHeight();
            const int buttonCount = 6;
            var itemSpacing = ImGui.GetStyle().ItemSpacing.X;
            ImGui.SetNextItemWidth(-((buttonSize * buttonCount) + (itemSpacing * (buttonCount + 2))));
            if (ImGui.BeginCombo("##profile", ProfileSelectionLabel(selectedProfileId)))
            {
                if (ImGui.Selectable(ProfileSelectionLabel(Guid.Empty), selectedProfileId == Guid.Empty))
                {
                    selectedProfileId = Guid.Empty;
                }
                configuration.Profiles.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
                foreach (var profile in configuration.Profiles)
                {
                    if (ImGui.Selectable(ProfileSelectionLabel(profile.Id), selectedProfileId == profile.Id))
                    {
                        selectedProfileId = profile.Id;
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.SameLine();
            if (selectedProfileId == configuration.DefaultProfileId)
            {
                ImGui.BeginDisabled();
            }
            if (DrawIconButton(FontAwesomeIcon.Star, "default_profile", selectedProfileId == configuration.DefaultProfileId ? "Default profile" : "Use as default profile", buttonSize))
            {
                setDefaultProfile = true;
            }
            if (selectedProfileId == configuration.DefaultProfileId)
            {
                ImGui.EndDisabled();
            }
            ImGui.SameLine(0f, itemSpacing * 2f);
            if (DrawIconButton(FontAwesomeIcon.Plus, "new_profile", "New profile", buttonSize))
            {
                openNewProfile = true;
            }
            ImGui.SameLine();
            if (DrawIconButton(FontAwesomeIcon.Clone, "duplicate_profile", "Duplicate profile", buttonSize))
            {
                openDuplicateProfile = true;
            }
            ImGui.SameLine();
            if (selectedProfileId == Guid.Empty)
            {
                ImGui.BeginDisabled();
            }
            if (DrawIconButton(FontAwesomeIcon.Pen, "rename_profile", selectedProfileId == Guid.Empty ? "Default cannot be renamed" : "Rename profile", buttonSize))
            {
                openRenameProfile = true;
            }
            if (selectedProfileId == Guid.Empty)
            {
                ImGui.EndDisabled();
            }
            ImGui.SameLine(0f, itemSpacing * 2f);
            if (DrawIconButton(FontAwesomeIcon.UndoAlt, "reset_profile", "Reset profile to defaults", buttonSize))
            {
                openResetProfile = true;
            }
            ImGui.SameLine();
            if (selectedProfileId == Guid.Empty)
            {
                ImGui.BeginDisabled();
            }
            if (DrawIconButton(FontAwesomeIcon.Trash, "delete_profile", selectedProfileId == Guid.Empty ? "Default cannot be deleted" : "Delete profile", buttonSize))
            {
                openDeleteProfile = true;
            }
            if (selectedProfileId == Guid.Empty)
            {
                ImGui.EndDisabled();
            }
            ImGui.EndTable();
        }
        ImGui.Separator();
        if (openNewProfile)
        {
            newProfileName = string.Empty;
            newProfileSourceId = Guid.Empty;
            ImGui.OpenPopup("New profile");
        }
        if (openDuplicateProfile)
        {
            var sourceName = selectedProfileId == Guid.Empty ? "Default" : ProfileName(selectedProfileId);
            newProfileName = sourceName[..Math.Min(sourceName.Length, 59)] + " copy";
            newProfileSourceId = selectedProfileId;
            ImGui.OpenPopup("New profile");
        }
        if (openRenameProfile)
        {
            renameProfileName = ProfileName(selectedProfileId);
            ImGui.OpenPopup("Rename profile");
        }
        if (setDefaultProfile)
        {
            profiles.SetDefault(selectedProfileId);
            configuration.Save();
        }
        if (openDeleteProfile)
        {
            ImGui.OpenPopup("Delete this profile?");
        }
        if (openResetProfile)
        {
            ImGui.OpenPopup("Reset this profile?");
        }
        if (ImGui.BeginPopupModal("Reset this profile?", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextUnformatted($"Reset {(selectedProfileId == Guid.Empty ? "Default" : ProfileName(selectedProfileId))} to its original settings?");
            if (ImGui.Button("Reset"))
            {
                Settings.Reset();
                configuration.Save();
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
        if (selectedProfileId != Guid.Empty)
        {
            var selected = FindProfile(selectedProfileId)!;
            if (ImGui.BeginPopupModal("Delete this profile?", ImGuiWindowFlags.AlwaysAutoResize))
            {
                var count = CountAssignments(selectedProfileId);
                ImGui.TextUnformatted($"Delete {selected.Name} and {count} location {(count == 1 ? "assignment" : "assignments")}?");
                if (ImGui.Button("Delete"))
                {
                    profiles.Delete(selectedProfileId);
                    selectedProfileId = Guid.Empty;
                    configuration.Save();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }
        DrawNewProfilePopup();
        DrawRenameProfilePopup();
    }

    private static bool DrawIconButton(FontAwesomeIcon icon, string id, string tooltip, float size)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        var pressed = ImGui.Button($"{icon.ToIconString()}##{id}", new Vector2(size, size));
        ImGui.PopFont();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(tooltip);
        }
        return pressed;
    }

    private void DrawNewProfilePopup()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(14f, 12f));
        if (!ImGui.BeginPopup("New profile"))
        {
            ImGui.PopStyleVar();
            return;
        }
        ImGui.TextUnformatted("New profile");
        ImGui.Spacing();
        if (BeginPopupForm("new_profile_fields"))
        {
            PopupFormLabel("Name");
            ImGui.SetNextItemWidth(-1f);
            ImGui.InputTextWithHint("##new_profile_name", "Profile name", ref newProfileName, 64);
            ImGui.EndTable();
        }
        var valid = IsUniqueName(newProfileName, Guid.Empty);
        if (!valid && newProfileName.Trim().Length != 0)
        {
            ImGui.TextDisabled("Choose a different name.");
        }
        ImGui.Spacing();
        AlignPopupActions("Cancel", "Create");
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (!valid)
        {
            ImGui.BeginDisabled();
        }
        if (ImGui.Button("Create"))
        {
            var source = newProfileSourceId == Guid.Empty ? configuration : FindProfile(newProfileSourceId)!.Settings;
            var profile = profiles.Create(newProfileName, source);
            selectedProfileId = profile.Id;
            configuration.Save();
            ImGui.CloseCurrentPopup();
        }
        if (!valid)
        {
            ImGui.EndDisabled();
        }
        ImGui.EndPopup();
        ImGui.PopStyleVar();
    }

    private void DrawRenameProfilePopup()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(14f, 12f));
        if (!ImGui.BeginPopup("Rename profile"))
        {
            ImGui.PopStyleVar();
            return;
        }
        ImGui.TextUnformatted("Rename profile");
        ImGui.Spacing();
        if (BeginPopupForm("rename_profile_fields"))
        {
            PopupFormLabel("Name");
            ImGui.SetNextItemWidth(-1f);
            ImGui.InputText("##rename_profile_name", ref renameProfileName, 64);
            ImGui.EndTable();
        }
        var valid = IsUniqueName(renameProfileName, selectedProfileId);
        ImGui.Spacing();
        AlignPopupActions("Cancel", "Rename");
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (!valid)
        {
            ImGui.BeginDisabled();
        }
        if (ImGui.Button("Rename"))
        {
            FindProfile(selectedProfileId)!.Name = renameProfileName.Trim();
            configuration.Save();
            ImGui.CloseCurrentPopup();
        }
        if (!valid)
        {
            ImGui.EndDisabled();
        }
        ImGui.EndPopup();
        ImGui.PopStyleVar();
    }

    private static bool BeginPopupForm(string id)
    {
        var flags = ImGuiTableFlags.PadOuterX | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings;
        if (!ImGui.BeginTable(id, 2, flags))
        {
            return false;
        }
        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 112f);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, 220f);
        return true;
    }

    private static void PopupFormLabel(string label)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.TableSetColumnIndex(1);
    }

    private static void AlignPopupActions(string first, string second)
    {
        var style = ImGui.GetStyle();
        var width = ImGui.CalcTextSize(first).X + ImGui.CalcTextSize(second).X + (style.FramePadding.X * 4f) + style.ItemSpacing.X;
        AlignNextItemRight(width);
    }

    private static void AlignNextItemRight(float width)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0f, ImGui.GetContentRegionAvail().X - width));
    }

    private void DrawAssignments()
    {
        InitializeAssignmentDraft();
        configuration.Assignments.Sort(static (left, right) => CompareAssignmentDisplayOrder(left, right));
        var draftConflict = IsValidDraftTarget() && HasDuplicate(TargetScope(draftScope, draftTargetId), draftTargetId, -1);
        ImGui.TextUnformatted($"Currently active: {(profiles.ActiveProfileId == Guid.Empty ? "Default" : ProfileName(profiles.ActiveProfileId))}");
        DrawHint("Zone, PvE duty, and PvP assignments use separate location sets.");
        var flags = ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings;
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(8f, 6f));
        var rowHeight = ImGui.GetFrameHeight() + (ImGui.GetStyle().CellPadding.Y * 2f);
        var savedRows = Math.Max(1, configuration.Assignments.Count);
        var naturalHeight = rowHeight * (savedRows + 1);
        var errorHeight = draftConflict ? ImGui.GetTextLineHeightWithSpacing() : 0f;
        var reservedHeight = rowHeight + errorHeight + ImGui.GetStyle().ItemSpacing.Y;
        var maximumHeight = Math.Max(1f, ImGui.GetContentRegionAvail().Y - reservedHeight);
        var tableHeight = Math.Min(naturalHeight, maximumHeight);
        var scrollFlags = naturalHeight > tableHeight ? ImGuiWindowFlags.AlwaysVerticalScrollbar : ImGuiWindowFlags.None;
        ImGui.BeginChild("assignment_scroll", new Vector2(0f, tableHeight), false, scrollFlags);
        var tableStart = ImGui.GetCursorScreenPos();
        var tableWidth = ImGui.GetContentRegionAvail().X;
        if (ImGui.BeginTable("assignment_map", 4, flags))
        {
            SetupAssignmentColumns(true);
            for (var index = 0; index < configuration.Assignments.Count; index++)
            {
                var assignment = configuration.Assignments[index];
                ImGui.PushID(index);
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                DrawSavedScope(assignment, index);
                ImGui.TableSetColumnIndex(1);
                DrawSavedLocation(assignment, index);
                ImGui.TableSetColumnIndex(2);
                var profileId = assignment.ProfileId;
                ImGui.SetNextItemWidth(-1f);
                if (DrawProfileCombo("profile", ref profileId))
                {
                    assignment.ProfileId = profileId;
                    SaveAssignments();
                }
                ImGui.TableSetColumnIndex(3);
                var actionSize = ImGui.GetFrameHeight();
                AlignNextItemRight(actionSize);
                if (ImGui.Button("×", new Vector2(actionSize, actionSize)))
                {
                    configuration.Assignments.RemoveAt(index--);
                    SaveAssignments();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Remove assignment");
                }
                ImGui.PopID();
            }
            if (configuration.Assignments.Count == 0)
            {
                const string message = "No assignments yet.";
                var emptyHeight = ImGui.GetFrameHeight() + (ImGui.GetStyle().CellPadding.Y * 2f);
                var emptyStart = ImGui.GetCursorScreenPos();
                var size = ImGui.CalcTextSize(message);
                ImGui.TableNextRow(ImGuiTableRowFlags.None, emptyHeight);
                ImGui.TableSetColumnIndex(0);
                var drawList = ImGui.GetWindowDrawList();
                drawList.PushClipRect(new Vector2(tableStart.X, emptyStart.Y), new Vector2(tableStart.X + tableWidth, emptyStart.Y + emptyHeight), false);
                drawList.AddText(new Vector2(tableStart.X + Math.Max(0f, (tableWidth - size.X) / 2f), emptyStart.Y + ((emptyHeight - size.Y) / 2f)), ImGui.GetColorU32(ImGuiCol.TextDisabled), message);
                drawList.PopClipRect();
            }
            ImGui.EndTable();
        }
        ImGui.EndChild();
        var draftFlags = ImGuiTableFlags.PadOuterX | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings;
        if (ImGui.BeginTable("assignment_draft", 4, draftFlags))
        {
            SetupAssignmentColumns(false);
            DrawAssignmentDraft(draftConflict);
            ImGui.EndTable();
        }
        if (draftConflict)
        {
            ImGui.TextColored(new Vector4(1f, 0.35f, 0.35f, 1f), "This location already has an assignment.");
        }
        ImGui.PopStyleVar();
    }

    private void DrawSavedScope(CursorAssignment assignment, int index)
    {
        ImGui.SetNextItemWidth(-1f);
        var currentScope = DisplayScope(assignment.Scope);
        if (!ImGui.BeginCombo("##scope", AssignmentScopeLabel(currentScope)))
        {
            return;
        }
        foreach (var scope in AssignmentScopes)
        {
            var targetId = CurrentTarget(scope);
            var targetScope = TargetScope(scope, targetId);
            var duplicate = scope == AssignmentScope.Territory && targetId == 0 || HasDuplicate(targetScope, targetId, index);
            if (duplicate && scope != currentScope)
            {
                ImGui.BeginDisabled();
            }
            if (ImGui.Selectable(AssignmentScopeLabel(scope), scope == currentScope) && scope != currentScope && !duplicate)
            {
                TryUpdateAssignment(index, scope, targetId);
            }
            if (duplicate && scope != currentScope)
            {
                ImGui.EndDisabled();
            }
        }
        ImGui.EndCombo();
    }

    private void DrawSavedLocation(CursorAssignment assignment, int index)
    {
        ImGui.SetNextItemWidth(-1f);
        SetLocationPopupSize();
        if (!ImGui.BeginCombo("##location", TargetName(assignment)))
        {
            return;
        }
        var shown = 0;
        var scope = DisplayScope(assignment.Scope);
        if (scope == AssignmentScope.Duty)
        {
            DrawAnyLocation(assignment, index, AssignmentScope.Duty);
            foreach (var entry in catalog.Duties)
            {
                if (shown++ == 200)
                {
                    continue;
                }
                var duplicate = HasDuplicate(AssignmentScope.Duty, entry.DutyGroupId, index);
                if (duplicate)
                {
                    ImGui.BeginDisabled();
                }
                if (ImGui.Selectable($"{entry.DutyName}  ({entry.TerritoryName})##{entry.DutyGroupId}", assignment.TargetId == entry.DutyGroupId) && !duplicate)
                {
                    TryUpdateAssignment(index, AssignmentScope.Duty, entry.DutyGroupId);
                }
                if (duplicate)
                {
                    ImGui.EndDisabled();
                }
            }
        }
        else if (scope == AssignmentScope.PvP)
        {
            DrawAnyLocation(assignment, index, AssignmentScope.PvP);
            foreach (var entry in catalog.Pvp)
            {
                if (shown++ == 200)
                {
                    break;
                }
                var duplicate = HasDuplicate(AssignmentScope.PvP, entry.PvpGroupId, index);
                if (duplicate)
                {
                    ImGui.BeginDisabled();
                }
                if (ImGui.Selectable($"{entry.Name}##{entry.PvpGroupId}", assignment.Scope == AssignmentScope.PvP && assignment.TargetId == entry.PvpGroupId) && !duplicate)
                {
                    TryUpdateAssignment(index, AssignmentScope.PvP, entry.PvpGroupId);
                }
                if (duplicate)
                {
                    ImGui.EndDisabled();
                }
            }
        }
        else
        {
            foreach (var zone in catalog.Zones)
            {
                if (shown++ == 200)
                {
                    break;
                }
                var duplicate = HasDuplicate(assignment.Scope, zone.ZoneId, index);
                if (duplicate)
                {
                    ImGui.BeginDisabled();
                }
                if (ImGui.Selectable($"{zone.Name}##{zone.ZoneId}", assignment.TargetId == zone.ZoneId) && !duplicate)
                {
                    TryUpdateAssignment(index, assignment.Scope, zone.ZoneId);
                }
                if (duplicate)
                {
                    ImGui.EndDisabled();
                }
            }
        }
        ImGui.EndCombo();
    }

    private void DrawAssignmentDraft(bool conflict)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        PushDraftConflictStyle(conflict);
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.BeginCombo("##draft_scope", AssignmentScopeLabel(draftScope)))
        {
            foreach (var scope in AssignmentScopes)
            {
                if (ImGui.Selectable(AssignmentScopeLabel(scope), scope == draftScope) && scope != draftScope)
                {
                    draftScope = scope;
                    draftTargetId = scope switch
                    {
                        AssignmentScope.Duty => draftDutyId,
                        AssignmentScope.PvP => draftPvpId,
                        _ => draftZoneId
                    };
                    locationSearch = string.Empty;
                }
            }
            ImGui.EndCombo();
        }
        PopDraftConflictStyle(conflict);
        ImGui.TableSetColumnIndex(1);
        PushDraftConflictStyle(conflict);
        DrawDraftLocation();
        PopDraftConflictStyle(conflict);
        ImGui.TableSetColumnIndex(2);
        ImGui.SetNextItemWidth(-1f);
        DrawProfileCombo("draft_profile", ref draftProfileId);
        ImGui.TableSetColumnIndex(3);
        if (!IsValidDraftTarget() || conflict)
        {
            ImGui.BeginDisabled();
        }
        var addWidth = ImGui.CalcTextSize("Add").X + (ImGui.GetStyle().FramePadding.X * 2f);
        AlignNextItemRight(addWidth);
        if (ImGui.Button("Add"))
        {
            TryAddDraftAssignment();
        }
        if (!IsValidDraftTarget() || conflict)
        {
            ImGui.EndDisabled();
        }
    }

    private void DrawDraftLocation()
    {
        var preview = draftTargetId == 0 && draftScope == AssignmentScope.Territory ? "Select a zone" : TargetName(TargetScope(draftScope, draftTargetId), draftTargetId);
        ImGui.SetNextItemWidth(-1f);
        SetLocationPopupSize();
        if (!ImGui.BeginCombo("##draft_location", preview))
        {
            return;
        }
        if (ImGui.IsWindowAppearing())
        {
            ImGui.SetKeyboardFocusHere();
        }
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##location_search", draftScope == AssignmentScope.Duty ? "Search duties..." : draftScope == AssignmentScope.PvP ? "Search PvP locations..." : "Search zones...", ref locationSearch, 128);
        var shown = 0;
        if (draftScope == AssignmentScope.Duty)
        {
            DrawDraftAny(AssignmentScope.Duty);
            foreach (var entry in catalog.Duties)
            {
                if (locationSearch.Length != 0 && !entry.DutyName.Contains(locationSearch, StringComparison.OrdinalIgnoreCase) && !entry.TerritoryName.Contains(locationSearch, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (DrawLocationCandidate(entry.DutyGroupId, $"{entry.DutyName}  ({entry.TerritoryName})", ref shown))
                {
                    break;
                }
            }
        }
        else if (draftScope == AssignmentScope.PvP)
        {
            DrawDraftAny(AssignmentScope.PvP);
            foreach (var entry in catalog.Pvp)
            {
                if (locationSearch.Length != 0 && !entry.SearchText.Contains(locationSearch, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (DrawLocationCandidate(entry.PvpGroupId, entry.Name, ref shown))
                {
                    break;
                }
            }
        }
        else
        {
            foreach (var zone in catalog.Zones)
            {
                if (locationSearch.Length != 0 && !zone.SearchText.Contains(locationSearch, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (DrawLocationCandidate(zone.ZoneId, zone.Name, ref shown))
                {
                    break;
                }
            }
        }
        if (shown == 200)
        {
            ImGui.TextDisabled("Type more to narrow the list.");
        }
        ImGui.EndCombo();
    }

    private bool DrawLocationCandidate(uint id, string label, ref int shown)
    {
        if (HasDuplicate(TargetScope(draftScope, id), id, -1))
        {
            return false;
        }
        if (shown == 200)
        {
            return true;
        }
        shown++;
        if (ImGui.Selectable($"{label}##{id}", draftTargetId == id))
        {
            draftTargetId = id;
            if (draftScope == AssignmentScope.Duty)
            {
                draftDutyId = id;
            }
            else if (draftScope == AssignmentScope.PvP)
            {
                draftPvpId = id;
            }
            else
            {
                draftZoneId = id;
            }
        }
        return false;
    }

    private static void PushDraftConflictStyle(bool conflict)
    {
        if (!conflict)
        {
            return;
        }
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1f, 0.3f, 0.3f, 1f));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
    }

    private static void PopDraftConflictStyle(bool conflict)
    {
        if (!conflict)
        {
            return;
        }
        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
    }

    private bool DrawProfileCombo(string id, ref Guid profileId)
    {
        var changed = false;
        if (!ImGui.BeginCombo($"##{id}", profileId == Guid.Empty ? "Default" : ProfileName(profileId)))
        {
            return false;
        }
        if (ImGui.Selectable("Default", profileId == Guid.Empty))
        {
            profileId = Guid.Empty;
            changed = true;
        }
        foreach (var profile in configuration.Profiles)
        {
            if (ImGui.Selectable(profile.Name, profileId == profile.Id))
            {
                profileId = profile.Id;
                changed = true;
            }
        }
        ImGui.EndCombo();
        return changed;
    }

    private static void SetupAssignmentColumns(bool headers)
    {
        ImGui.TableSetupColumn("Scope", ImGuiTableColumnFlags.WidthFixed, 70f);
        ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.WidthStretch, 1.5f);
        ImGui.TableSetupColumn("Profile", ImGuiTableColumnFlags.WidthStretch, 1f);
        ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 54f);
        if (headers)
        {
            ImGui.TableHeadersRow();
        }
    }

    private static void SetLocationPopupSize()
    {
        var width = Math.Max(240f, ImGui.CalcItemWidth());
        ImGui.SetNextWindowSizeConstraints(new Vector2(width, 0f), new Vector2(width, 300f));
    }

    private void InitializeAssignmentDraft()
    {
        if (!assignmentDraftInitialized)
        {
            assignmentDraftInitialized = true;
            ResetAssignmentDraft();
        }
    }

    private void ResetAssignmentDraft()
    {
        var territoryId = Plugin.ClientState.TerritoryType;
        var currentZoneId = catalog.GetZoneGroup(territoryId);
        var currentDutyId = catalog.GetDutyGroup(Plugin.DutyState.ContentFinderCondition.RowId);
        var currentPvpId = catalog.GetPvpGroup(territoryId);
        draftScope = Plugin.ClientState.IsPvP || currentPvpId != 0 ? AssignmentScope.PvP : currentDutyId == 0 ? AssignmentScope.Territory : AssignmentScope.Duty;
        draftZoneId = HasDuplicate(AssignmentScope.Territory, currentZoneId, -1) ? 0 : currentZoneId;
        draftDutyId = HasDuplicate(TargetScope(AssignmentScope.Duty, currentDutyId), currentDutyId, -1) ? 0 : currentDutyId;
        draftPvpId = !HasDuplicate(AssignmentScope.PvPAny, 0, -1)
            ? 0
            : HasDuplicate(AssignmentScope.PvP, currentPvpId, -1) ? 0 : currentPvpId;
        draftTargetId = CurrentDraftTarget();
        draftProfileId = Guid.Empty;
        locationSearch = string.Empty;
    }

    private bool HasDuplicate(AssignmentScope scope, uint targetId, int exceptIndex)
    {
        for (var index = 0; index < configuration.Assignments.Count; index++)
        {
            if (index != exceptIndex && configuration.Assignments[index].Scope == scope && configuration.Assignments[index].TargetId == targetId)
            {
                return true;
            }
        }
        return false;
    }

    private void DrawAnyLocation(CursorAssignment assignment, int index, AssignmentScope scope)
    {
        var anyScope = TargetScope(scope, 0);
        var duplicate = HasDuplicate(anyScope, 0, index);
        if (duplicate)
        {
            ImGui.BeginDisabled();
        }
        if (ImGui.Selectable("Any", assignment.Scope == anyScope) && !duplicate)
        {
            TryUpdateAssignment(index, scope, 0);
        }
        if (duplicate)
        {
            ImGui.EndDisabled();
        }
    }

    private void DrawDraftAny(AssignmentScope scope)
    {
        var anyScope = TargetScope(scope, 0);
        var duplicate = HasDuplicate(anyScope, 0, -1);
        if (duplicate)
        {
            ImGui.BeginDisabled();
        }
        if (ImGui.Selectable("Any", draftTargetId == 0) && !duplicate)
        {
            draftTargetId = 0;
            if (scope == AssignmentScope.Duty)
            {
                draftDutyId = 0;
            }
            else
            {
                draftPvpId = 0;
            }
        }
        if (duplicate)
        {
            ImGui.EndDisabled();
        }
    }

    private uint CurrentTarget(AssignmentScope scope)
    {
        return scope switch
        {
            AssignmentScope.Duty => catalog.GetDutyGroup(Plugin.DutyState.ContentFinderCondition.RowId),
            AssignmentScope.PvP => catalog.GetPvpGroup(Plugin.ClientState.TerritoryType),
            _ => catalog.GetZoneGroup(Plugin.ClientState.TerritoryType)
        };
    }

    private uint CurrentDraftTarget()
    {
        return draftScope switch
        {
            AssignmentScope.Duty => draftDutyId,
            AssignmentScope.PvP => draftPvpId,
            _ => draftZoneId
        };
    }

    private bool IsValidDraftTarget() => draftScope != AssignmentScope.Territory || draftTargetId != 0;

    private static AssignmentScope DisplayScope(AssignmentScope scope)
    {
        return scope switch
        {
            AssignmentScope.DutyAny => AssignmentScope.Duty,
            AssignmentScope.PvPAny => AssignmentScope.PvP,
            _ => scope
        };
    }

    private static AssignmentScope TargetScope(AssignmentScope scope, uint targetId)
    {
        scope = DisplayScope(scope);
        return targetId != 0 ? scope : scope switch
        {
            AssignmentScope.Duty => AssignmentScope.DutyAny,
            AssignmentScope.PvP => AssignmentScope.PvPAny,
            _ => scope
        };
    }

    private static int CompareAssignmentDisplayOrder(CursorAssignment left, CursorAssignment right)
    {
        var order = AssignmentDisplayOrder(left.Scope).CompareTo(AssignmentDisplayOrder(right.Scope));
        return order != 0 ? order : left.TargetId.CompareTo(right.TargetId);
    }

    private static int AssignmentDisplayOrder(AssignmentScope scope)
    {
        return scope switch
        {
            AssignmentScope.PvPAny => 0,
            AssignmentScope.DutyAny => 1,
            AssignmentScope.PvP => 2,
            AssignmentScope.Duty => 3,
            _ => 4
        };
    }

    private bool TryAddDraftAssignment()
    {
        var scope = TargetScope(draftScope, draftTargetId);
        if (!IsValidDraftTarget() || HasDuplicate(scope, draftTargetId, -1))
        {
            return false;
        }
        configuration.Assignments.Add(new CursorAssignment { Scope = scope, TargetId = draftTargetId, ProfileId = draftProfileId });
        SaveAssignments();
        ResetAssignmentDraft();
        return true;
    }

    private bool TryUpdateAssignment(int index, AssignmentScope scope, uint targetId)
    {
        scope = TargetScope(scope, targetId);
        if ((uint)index >= (uint)configuration.Assignments.Count || scope == AssignmentScope.Territory && targetId == 0 || HasDuplicate(scope, targetId, index))
        {
            return false;
        }
        var assignment = configuration.Assignments[index];
        assignment.Scope = scope;
        assignment.TargetId = targetId;
        SaveAssignments();
        return true;
    }

    private void SaveAssignments()
    {
        profiles.Rebuild();
        configuration.Save();
    }

    private CursorProfile? FindProfile(Guid id)
    {
        for (var index = 0; index < configuration.Profiles.Count; index++)
        {
            if (configuration.Profiles[index].Id == id)
            {
                return configuration.Profiles[index];
            }
        }
        return null;
    }

    private string ProfileName(Guid id) => FindProfile(id)?.Name ?? "Unknown profile";

    private string ProfileSelectionLabel(Guid id)
    {
        var name = id == Guid.Empty ? "Default" : ProfileName(id);
        return id == configuration.DefaultProfileId ? $"★ {name}" : name;
    }

    private bool IsUniqueName(string name, Guid except)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }
        for (var index = 0; index < configuration.Profiles.Count; index++)
        {
            var profile = configuration.Profiles[index];
            if (profile.Id != except && string.Equals(profile.Name, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        return true;
    }

    private int CountAssignments(Guid profileId)
    {
        var count = 0;
        for (var index = 0; index < configuration.Assignments.Count; index++)
        {
            if (configuration.Assignments[index].ProfileId == profileId)
            {
                count++;
            }
        }
        return count;
    }

    private string TargetName(CursorAssignment assignment)
    {
        return TargetName(assignment.Scope, assignment.TargetId);
    }

    private string TargetName(AssignmentScope scope, uint targetId)
    {
        if (scope == AssignmentScope.Territory)
        {
            return catalog.GetZoneName(targetId);
        }
        if (scope == AssignmentScope.PvPAny || scope == AssignmentScope.DutyAny)
        {
            return "Any";
        }
        if (scope == AssignmentScope.PvP)
        {
            return catalog.GetPvpName(targetId);
        }
        foreach (var entry in catalog.Duties)
        {
            if (entry.DutyGroupId == targetId)
            {
                return entry.DutyName;
            }
        }
        return $"Unknown ({targetId})";
    }

    private static string AssignmentScopeLabel(AssignmentScope value) => value switch
    {
        AssignmentScope.Duty => "Duty",
        AssignmentScope.PvP => "PvP",
        _ => "Zone"
    };

#if CURSORRING_BENCHMARK
    internal RenderBenchmark Benchmark => benchmark;

    private void DrawBenchmark()
    {
        DrawHint("Measures the cursor render path for 10 seconds. Keep the ring visible and hover a targetable entity for representative results.");

        if (benchmark.Phase == BenchmarkPhase.Countdown)
        {
            ImGui.ProgressBar((float)benchmark.CountdownProgress, new Vector2(-1f, 0f), $"Starting in {benchmark.CountdownSecondsRemaining}");
            ImGui.TextUnformatted(Settings.ShowGcd
                ? "Prepare to use GCD actions and hover a targetable entity."
                : "Keep the cursor visible and hover a targetable entity.");
            if (ImGui.Button("Cancel benchmark"))
            {
                benchmark.Cancel();
            }
        }
        else if (benchmark.IsCollecting)
        {
            ImGui.ProgressBar((float)benchmark.Progress, new Vector2(-1f, 0f), $"Running: {benchmark.SampleCount} frames");
            ImGui.TextUnformatted(Settings.ShowGcd
                ? "Benchmark running. Continue using GCD actions."
                : "Benchmark running. Keep the cursor visible.");
            if (Settings.ShowGcd)
            {
                ImGui.TextUnformatted(benchmark.GcdDetected ? "GCD detected: yes" : "GCD detected: not yet");
                if (Settings.ShowCastSegments)
                {
                    ImGui.TextUnformatted(benchmark.CastSegmentsDetected ? "Cast segments detected: yes" : "Cast segments detected: not yet");
                }
            }
            if (Settings.ShowHoverIndicator)
            {
                ImGui.TextUnformatted(benchmark.HoverDetected ? "Hover detected: yes" : "Hover detected: not yet");
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

        if (Settings.ShowGcd)
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
        var available = ImGui.GetContentRegionAvail();
        var width = available.X;
        var previewHeight = Math.Max(1f, available.Y);
        var spacing = ImGui.GetStyle().ItemSpacing.Y;
        var variantHeight = Math.Max(1f, (previewHeight - spacing) / 2f);
        var labelHeight = ImGui.GetTextLineHeightWithSpacing();
        var canvasHeight = Math.Max(1f, variantHeight - labelHeight);
        var extent = MathF.Max(GetPreviewExtent(false), GetPreviewExtent(Settings.ShowHoverIndicator));
        var scale = MathF.Min(1f, MathF.Min((canvasHeight - 10f) / (extent * 2f), (width - 10f) / (extent * 2f)));
        DrawPreviewVariant("normal_preview", "Normal", false, width, variantHeight, scale);
        ImGui.Spacing();
        DrawPreviewVariant("hover_preview", Settings.ShowHoverIndicator ? "Hover" : "Hover (disabled)", Settings.ShowHoverIndicator, width, variantHeight, scale);
    }

    private float GetPreviewExtent(bool hovered)
    {
        var geometry = RingMath.GetGeometry(Settings);
        var ringBorder = Settings.ShowRingBorder ? Settings.RingBorderThickness : 0f;
        var dotBorder = Settings.ShowDotBorder ? Settings.DotBorderThickness : 0f;
        var gcdBorder = Settings.ShowGcd && Settings.ShowGcdBorder ? Settings.GcdBorderThickness : 0f;
        var ringExtent = geometry.Main + (Settings.RingThickness / 2f) + ringBorder;
        var dotExtent = hovered && Settings.HideDotOnHover ? 0f : (Settings.DotDiameter / 2f) + dotBorder;
        var gcdExtent = Settings.ShowGcd ? Settings.GcdPlacement switch
        {
            GcdPlacement.Outer => geometry.Outer + (Settings.GcdThickness / 2f) + gcdBorder,
            GcdPlacement.Inner => geometry.Inner + (geometry.InnerThickness / 2f) + geometry.InnerBorderThickness,
            GcdPlacement.Overlay when Settings.OverlayFill == OverlayFillStyle.Pie => geometry.Pie,
            _ => geometry.Main + (Settings.RingThickness / 2f) + gcdBorder
        } : 0f;
        var hoverExtent = hovered ? HoverIndicatorMath.GetGeometry(Settings).Extent : 0f;
        return MathF.Max(MathF.Max(ringExtent, dotExtent), MathF.Max(gcdExtent, hoverExtent));
    }

    private void DrawPreviewVariant(string id, string label, bool hovered, float width, float height, float scale)
    {
        ImGui.BeginChild(id, new Vector2(width, height), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        ImGui.TextDisabled(label);
        var start = ImGui.GetCursorScreenPos();
        var available = ImGui.GetContentRegionAvail();
        var center = start + (available / 2f);
        var previewProgress = Settings.ProgressBehavior == ProgressBehavior.Fill ? 0.9f : 0.35f;
        var gcd = Settings.ShowGcd ? new GcdState(true, previewProgress * 2.5f, 2.5f) : GcdState.Inactive;
        var segments = Settings.ShowGcd && Settings.ShowCastSegments ? new CastTimeline(true, gcd.Elapsed, gcd.Total, 0.55f, 0.78f, true) : CastTimeline.Inactive;
        CursorRenderer.DrawAt(Settings, ImGui.GetWindowDrawList(), center, gcd, segments, scale, hovered);
        ImGui.Dummy(available);
        ImGui.EndChild();
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
        return value switch
        {
            VisibilityMode.Always => "Always",
            VisibilityMode.CombatOnly => "In combat",
            VisibilityMode.DutyOnly => "In duty",
            VisibilityMode.DutyCombat => "In duty combat",
            _ => "In combat or duty"
        };
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

    private static string HoverVisibilityLabel(HoverVisibilityMode value)
    {
        return value switch
        {
            HoverVisibilityMode.OutOfCombatOnly => "Out of combat",
            HoverVisibilityMode.InCombatOnly => "In combat",
            _ => "Whenever cursor is visible"
        };
    }

    private static string HoverIndicatorStyleLabel(HoverIndicatorStyle value)
    {
        return value switch
        {
            HoverIndicatorStyle.Crosshair => "Crosshair",
            HoverIndicatorStyle.CornerBrackets => "Corner brackets",
            _ => "Inward carets"
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

}
