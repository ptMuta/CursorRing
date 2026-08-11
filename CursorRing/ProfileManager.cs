using System;
using System.Collections.Generic;

namespace CursorRing;

internal sealed class ProfileManager
{
    private readonly Configuration configuration;
    private readonly Dictionary<(AssignmentScope, uint), Guid> assignments = [];
    private readonly Dictionary<Guid, CursorProfile> profiles = [];
    private uint currentTerritoryId;
    private uint currentDutyGroupId;
    private uint currentPvpGroupId;
    private bool currentInDuty;
    private bool currentInPvP;

    internal ProfileManager(Configuration configuration)
    {
        this.configuration = configuration;
        ActiveSettings = configuration;
        Rebuild();
    }

    internal CursorSettings ActiveSettings { get; private set; }
    internal Guid ActiveProfileId { get; private set; }
    internal event Action? OnActiveChanged;

    internal bool Resolve(uint territoryId, uint dutyGroupId, uint pvpGroupId, bool inDuty, bool inPvP)
    {
        currentTerritoryId = territoryId;
        currentDutyGroupId = dutyGroupId;
        currentPvpGroupId = pvpGroupId;
        currentInDuty = inDuty;
        currentInPvP = inPvP;
        var id = ProfileRules.Resolve(assignments, territoryId, dutyGroupId, pvpGroupId, inDuty, inPvP, configuration.DefaultProfileId);
        CursorSettings settings;
        if (id == Guid.Empty)
        {
            settings = configuration;
        }
        else if (profiles.TryGetValue(id, out var profile))
        {
            settings = profile.Settings;
        }
        else
        {
            id = Guid.Empty;
            settings = configuration;
        }
        if (ReferenceEquals(settings, ActiveSettings) && id == ActiveProfileId)
        {
            return false;
        }
        ActiveProfileId = id;
        ActiveSettings = settings;
        OnActiveChanged?.Invoke();
        return true;
    }

    internal void Rebuild()
    {
        profiles.Clear();
        assignments.Clear();
        foreach (var profile in configuration.Profiles)
        {
            profiles.Add(profile.Id, profile);
        }
        foreach (var assignment in configuration.Assignments)
        {
            assignments[(assignment.Scope, assignment.TargetId)] = assignment.ProfileId;
        }
        Resolve(currentTerritoryId, currentDutyGroupId, currentPvpGroupId, currentInDuty, currentInPvP);
    }

    internal CursorProfile Create(string name, CursorSettings source)
    {
        var profile = new CursorProfile { Name = name, Settings = source.Copy() };
        configuration.Profiles.Add(profile);
        configuration.Normalize();
        Rebuild();
        return profile;
    }

    internal int Delete(Guid id)
    {
        var removed = configuration.Assignments.RemoveAll(value => value.ProfileId == id);
        configuration.Profiles.RemoveAll(value => value.Id == id);
        if (configuration.DefaultProfileId == id)
        {
            configuration.DefaultProfileId = Guid.Empty;
        }
        Rebuild();
        return removed;
    }

    internal void SetDefault(Guid id)
    {
        configuration.DefaultProfileId = id != Guid.Empty && !profiles.ContainsKey(id) ? Guid.Empty : id;
        Resolve(currentTerritoryId, currentDutyGroupId, currentPvpGroupId, currentInDuty, currentInPvP);
    }
}
