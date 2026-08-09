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
            if (assignment is null || !Enum.IsDefined(assignment.Scope) || assignment.TargetId == 0 || (assignment.ProfileId != Guid.Empty && !ids.Contains(assignment.ProfileId)) || !targets.Add((assignment.Scope, assignment.TargetId)))
            {
                assignments.RemoveAt(index);
                changed = true;
            }
        }
        return changed;
    }

    internal static Guid Resolve(IReadOnlyDictionary<uint, Guid> territories, IReadOnlyDictionary<uint, Guid> duties, uint territoryId, uint dutyGroupId, Guid defaultProfileId)
    {
        return duties.TryGetValue(dutyGroupId, out var duty) ? duty : territories.TryGetValue(territoryId, out var territory) ? territory : defaultProfileId;
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

    internal static bool NormalizeZoneTargets(List<CursorAssignment> assignments, IReadOnlyDictionary<uint, uint> groups)
    {
        var changed = false;
        var targets = new HashSet<(AssignmentScope, uint)>();
        for (var index = assignments.Count - 1; index >= 0; index--)
        {
            var assignment = assignments[index];
            if (assignment.Scope == AssignmentScope.Territory && groups.TryGetValue(assignment.TargetId, out var groupId) && assignment.TargetId != groupId)
            {
                assignment.TargetId = groupId;
                changed = true;
            }
            if (!targets.Add((assignment.Scope, assignment.TargetId)))
            {
                assignments.RemoveAt(index);
                changed = true;
            }
        }
        return changed;
    }
}
