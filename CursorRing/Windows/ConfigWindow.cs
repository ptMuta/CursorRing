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

    private static readonly AssignmentScope[] AssignmentScopes = [AssignmentScope.Territory, AssignmentScope.Duty];
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
    private Guid draftProfileId;
    private string locationSearch = string.Empty;
    private bool assignmentDraftInitialized;
    private ProfileDomain profileDomain;
#if CURSORRING_BENCHMARK
    private readonly RenderBenchmark benchmark;
#endif

    internal ConfigWindow(Configuration configuration, ProfileManager profiles, InstanceCatalog catalog)
        : base("CursorRing Settings###CursorRingConfig")
    {
        this.configuration = configuration;
        this.profiles = profiles;
        this.catalog = catalog;
#if CURSORRING_BENCHMARK
        benchmark = new RenderBenchmark();
#endif
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(720f, 520f),
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
        ImGui.BeginChild("profile_preview", new Vector2(0f, height), true);
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
        var draftConflict = draftTargetId != 0 && HasDuplicate(draftScope, draftTargetId, -1);
        ImGui.TextUnformatted($"Currently active: {(profiles.ActiveProfileId == Guid.Empty ? "Default" : ProfileName(profiles.ActiveProfileId))}");
        DrawHint("Duty assignments take priority over zone assignments.");
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
        if (!ImGui.BeginCombo("##scope", AssignmentScopeLabel(assignment.Scope)))
        {
            return;
        }
        foreach (var scope in AssignmentScopes)
        {
            var targetId = scope == AssignmentScope.Duty ? catalog.GetDutyGroup(Plugin.DutyState.ContentFinderCondition.RowId) : catalog.GetZoneGroup(Plugin.ClientState.TerritoryType);
            var duplicate = targetId == 0 || HasDuplicate(scope, targetId, index);
            if (duplicate && scope != assignment.Scope)
            {
                ImGui.BeginDisabled();
            }
            if (ImGui.Selectable(AssignmentScopeLabel(scope), scope == assignment.Scope) && scope != assignment.Scope && !duplicate)
            {
                TryUpdateAssignment(index, scope, targetId);
            }
            if (duplicate && scope != assignment.Scope)
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
        if (assignment.Scope == AssignmentScope.Duty)
        {
            foreach (var entry in catalog.Duties)
            {
                if (shown++ == 200)
                {
                    continue;
                }
                var duplicate = HasDuplicate(assignment.Scope, entry.DutyGroupId, index);
                if (duplicate)
                {
                    ImGui.BeginDisabled();
                }
                if (ImGui.Selectable($"{entry.DutyName}  ({entry.TerritoryName})##{entry.DutyGroupId}", assignment.TargetId == entry.DutyGroupId) && !duplicate)
                {
                    TryUpdateAssignment(index, assignment.Scope, entry.DutyGroupId);
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
                    draftTargetId = scope == AssignmentScope.Duty ? draftDutyId : draftZoneId;
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
        if (draftTargetId == 0 || conflict)
        {
            ImGui.BeginDisabled();
        }
        var addWidth = ImGui.CalcTextSize("Add").X + (ImGui.GetStyle().FramePadding.X * 2f);
        AlignNextItemRight(addWidth);
        if (ImGui.Button("Add"))
        {
            TryAddDraftAssignment();
        }
        if (draftTargetId == 0 || conflict)
        {
            ImGui.EndDisabled();
        }
    }

    private void DrawDraftLocation()
    {
        var preview = draftTargetId == 0 ? $"Select a {AssignmentScopeLabel(draftScope).ToLowerInvariant()}" : TargetName(draftScope, draftTargetId);
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
        ImGui.InputTextWithHint("##location_search", draftScope == AssignmentScope.Duty ? "Search duties..." : "Search zones...", ref locationSearch, 128);
        var shown = 0;
        if (draftScope == AssignmentScope.Duty)
        {
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
        if (HasDuplicate(draftScope, id, -1))
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
        var currentZoneId = catalog.GetZoneGroup(Plugin.ClientState.TerritoryType);
        var currentDutyId = catalog.GetDutyGroup(Plugin.DutyState.ContentFinderCondition.RowId);
        draftScope = currentDutyId == 0 ? AssignmentScope.Territory : AssignmentScope.Duty;
        draftZoneId = HasDuplicate(AssignmentScope.Territory, currentZoneId, -1) ? 0 : currentZoneId;
        draftDutyId = HasDuplicate(AssignmentScope.Duty, currentDutyId, -1) ? 0 : currentDutyId;
        draftTargetId = draftScope == AssignmentScope.Duty ? draftDutyId : draftZoneId;
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

    private bool TryAddDraftAssignment()
    {
        if (draftTargetId == 0 || HasDuplicate(draftScope, draftTargetId, -1))
        {
            return false;
        }
        configuration.Assignments.Add(new CursorAssignment { Scope = draftScope, TargetId = draftTargetId, ProfileId = draftProfileId });
        SaveAssignments();
        ResetAssignmentDraft();
        return true;
    }

    private bool TryUpdateAssignment(int index, AssignmentScope scope, uint targetId)
    {
        if ((uint)index >= (uint)configuration.Assignments.Count || targetId == 0 || HasDuplicate(scope, targetId, index))
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
        foreach (var entry in catalog.Duties)
        {
            if (entry.DutyGroupId == targetId)
            {
                return entry.DutyName;
            }
        }
        return $"Unknown ({targetId})";
    }

    private static string AssignmentScopeLabel(AssignmentScope value) => value == AssignmentScope.Duty ? "Duty" : "Zone";

#if CURSORRING_BENCHMARK
    internal RenderBenchmark Benchmark => benchmark;

    private void DrawBenchmark()
    {
        DrawHint("Measures the cursor render path for 10 seconds. Keep the ring visible for representative results.");

        if (benchmark.Phase == BenchmarkPhase.Countdown)
        {
            ImGui.ProgressBar((float)benchmark.CountdownProgress, new Vector2(-1f, 0f), $"Starting in {benchmark.CountdownSecondsRemaining}");
            ImGui.TextUnformatted(Settings.ShowGcd
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
        var start = ImGui.GetCursorScreenPos();
        var available = ImGui.GetContentRegionAvail();
        var width = available.X;
        var previewHeight = Math.Max(1f, available.Y);
        var center = start + new Vector2(width / 2f, previewHeight / 2f);
        var geometry = RingMath.GetGeometry(Settings);
        var ringBorder = Settings.ShowRingBorder ? Settings.RingBorderThickness : 0f;
        var dotBorder = Settings.ShowDotBorder ? Settings.DotBorderThickness : 0f;
        var gcdBorder = Settings.ShowGcd && Settings.ShowGcdBorder ? Settings.GcdBorderThickness : 0f;
        var ringExtent = geometry.Main + (Settings.RingThickness / 2f) + ringBorder;
        var dotExtent = (Settings.DotDiameter / 2f) + dotBorder;
        var gcdExtent = Settings.ShowGcd ? Settings.GcdPlacement switch
        {
            GcdPlacement.Outer => geometry.Outer + (Settings.GcdThickness / 2f) + gcdBorder,
            GcdPlacement.Inner => geometry.Inner + (geometry.InnerThickness / 2f) + geometry.InnerBorderThickness,
            GcdPlacement.Overlay when Settings.OverlayFill == OverlayFillStyle.Pie => geometry.Pie,
            _ => geometry.Main + (Settings.RingThickness / 2f) + gcdBorder
        } : 0f;
        var extent = MathF.Max(MathF.Max(ringExtent, dotExtent), gcdExtent);
        var scale = MathF.Min(1f, MathF.Min((previewHeight - 10f) / (extent * 2f), (width - 10f) / (extent * 2f)));
        var previewProgress = Settings.ProgressBehavior == ProgressBehavior.Fill ? 0.9f : 0.35f;
        var gcd = Settings.ShowGcd ? new GcdState(true, previewProgress * 2.5f, 2.5f) : GcdState.Inactive;
        var segments = Settings.ShowGcd && Settings.ShowCastSegments ? new CastTimeline(true, gcd.Elapsed, gcd.Total, 0.55f, 0.78f, true) : CastTimeline.Inactive;
        CursorRenderer.DrawAt(Settings, ImGui.GetWindowDrawList(), center, gcd, segments, scale);
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

}
