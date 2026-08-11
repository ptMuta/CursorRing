using System;
using System.Collections.Generic;

namespace CursorRing;

internal readonly record struct DutyGroupKey(uint TerritoryId, string DutyName);

internal static class ProfileRules
{
    internal static bool Normalize(List<CursorProfile> profiles, List<CursorAssignment> assignments)
    {
        var changed = false;
        var ids = new HashSet<Guid>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < profiles.Count; index++)
        {
            var profile = profiles[index] ?? new CursorProfile();
            if (!ReferenceEquals(profile, profiles[index]))
            {
                profiles[index] = profile;
                changed = true;
            }
            if (profile.Id == Guid.Empty || !ids.Add(profile.Id))
            {
                do
                {
                    profile.Id = Guid.NewGuid();
                }
                while (!ids.Add(profile.Id));
                changed = true;
            }
            var originalName = profile.Name;
            var name = (profile.Name ?? string.Empty).Trim();
            if (name.Length > 64)
            {
                name = name[..64].TrimEnd();
            }
            if (name.Length == 0)
            {
                name = "Profile";
            }
            var baseName = name;
            var suffix = 2;
            while (!names.Add(name))
            {
                var marker = $" {suffix++}";
                name = baseName[..Math.Min(baseName.Length, 64 - marker.Length)] + marker;
            }
            if (!string.Equals(originalName, name, StringComparison.Ordinal))
            {
                profile.Name = name;
                changed = true;
            }
            if (profile.Settings is null)
            {
                profile.Settings = new CursorSettings();
                changed = true;
            }
            changed |= profile.Settings.Normalize();
        }
        var targets = new HashSet<(AssignmentScope, uint)>();
        for (var index = assignments.Count - 1; index >= 0; index--)
        {
            var assignment = assignments[index];
            if (assignment is null || !IsValidTarget(assignment.Scope, assignment.TargetId) || (assignment.ProfileId != Guid.Empty && !ids.Contains(assignment.ProfileId)) || !targets.Add((assignment.Scope, assignment.TargetId)))
            {
                assignments.RemoveAt(index);
                changed = true;
            }
        }
        return changed;
    }

    internal static Guid Resolve(IReadOnlyDictionary<(AssignmentScope, uint), Guid> assignments, uint territoryId, uint dutyGroupId, uint pvpGroupId, bool inDuty, bool inPvP, Guid defaultProfileId)
    {
        if (inPvP)
        {
            return pvpGroupId != 0 && assignments.TryGetValue((AssignmentScope.PvP, pvpGroupId), out var pvp) ? pvp
                : assignments.TryGetValue((AssignmentScope.PvPAny, 0), out var pvpAny) ? pvpAny
                : defaultProfileId;
        }
        if (inDuty)
        {
            return dutyGroupId != 0 && assignments.TryGetValue((AssignmentScope.Duty, dutyGroupId), out var duty) ? duty
                : assignments.TryGetValue((AssignmentScope.DutyAny, 0), out var dutyAny) ? dutyAny
                : defaultProfileId;
        }
        return territoryId != 0 && assignments.TryGetValue((AssignmentScope.Territory, territoryId), out var territory) ? territory : defaultProfileId;
    }

    internal static Guid NormalizeDefaultProfileId(IReadOnlyList<CursorProfile> profiles, Guid id)
    {
        if (id == Guid.Empty)
        {
            return Guid.Empty;
        }
        for (var index = 0; index < profiles.Count; index++)
        {
            if (profiles[index].Id == id)
            {
                return id;
            }
        }
        return Guid.Empty;
    }

    internal static bool NormalizeCatalogTargets(
        List<CursorAssignment> assignments,
        IReadOnlyDictionary<uint, uint> zones,
        IReadOnlyDictionary<uint, uint> duties,
        IReadOnlyDictionary<uint, uint> pvpDuties,
        IReadOnlySet<uint> contextualTerritories)
    {
        var changed = false;
        var targets = new HashSet<(AssignmentScope, uint)>();
        for (var index = assignments.Count - 1; index >= 0; index--)
        {
            var assignment = assignments[index];
            if (assignment.Scope == AssignmentScope.Territory)
            {
                if (zones.TryGetValue(assignment.TargetId, out var zoneGroupId))
                {
                    changed |= UpdateTarget(assignment, AssignmentScope.Territory, zoneGroupId);
                }
                else if (contextualTerritories.Contains(assignment.TargetId))
                {
                    assignments.RemoveAt(index);
                    changed = true;
                    continue;
                }
            }
            else if (assignment.Scope == AssignmentScope.Duty)
            {
                if (pvpDuties.TryGetValue(assignment.TargetId, out var pvpGroupId))
                {
                    changed |= UpdateTarget(assignment, AssignmentScope.PvP, pvpGroupId);
                }
                else if (duties.TryGetValue(assignment.TargetId, out var dutyGroupId))
                {
                    changed |= UpdateTarget(assignment, AssignmentScope.Duty, dutyGroupId);
                }
            }
            if (!targets.Add((assignment.Scope, assignment.TargetId)))
            {
                assignments.RemoveAt(index);
                changed = true;
            }
        }
        return changed;
    }

    private static bool IsValidTarget(AssignmentScope scope, uint targetId)
    {
        return scope switch
        {
            AssignmentScope.Territory or AssignmentScope.Duty or AssignmentScope.PvP => targetId != 0,
            AssignmentScope.DutyAny or AssignmentScope.PvPAny => targetId == 0,
            _ => false
        };
    }

    private static bool UpdateTarget(CursorAssignment assignment, AssignmentScope scope, uint targetId)
    {
        if (assignment.Scope == scope && assignment.TargetId == targetId)
        {
            return false;
        }
        assignment.Scope = scope;
        assignment.TargetId = targetId;
        return true;
    }

}
