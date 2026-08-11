namespace CursorRing.Tests;

public sealed class ProfileRulesTests
{
    [Fact]
    public void CopyIsIndependent()
    {
        var source = new CursorSettings { RingDiameter = 80f };
        var copy = source.Copy();
        copy.RingDiameter = 20f;
        Assert.Equal(80f, source.RingDiameter);
    }

    [Fact]
    public void NormalizeRepairsProfilesAndAssignments()
    {
        var id = Guid.NewGuid();
        var profiles = new List<CursorProfile>
        {
            new() { Id = id, Name = " Same ", Settings = new CursorSettings { RingDiameter = float.NaN } },
            new() { Id = id, Name = "same" }
        };
        var assignments = new List<CursorAssignment>
        {
            new() { Scope = AssignmentScope.Territory, TargetId = 10, ProfileId = id },
            new() { Scope = AssignmentScope.Territory, TargetId = 10, ProfileId = id },
            new() { Scope = AssignmentScope.Duty, TargetId = 20, ProfileId = Guid.NewGuid() },
            new() { Scope = AssignmentScope.Duty, ProfileId = Guid.Empty }
        };
        Assert.True(ProfileRules.Normalize(profiles, assignments));
        Assert.NotEqual(profiles[0].Id, profiles[1].Id);
        Assert.Equal("Same", profiles[0].Name);
        Assert.Equal("same 2", profiles[1].Name);
        Assert.Equal(48f, profiles[0].Settings.RingDiameter);
        Assert.Single(assignments);
    }

    [Fact]
    public void DutyOverridesTerritoryIncludingExplicitDefault()
    {
        var territoryProfile = Guid.NewGuid();
        var assignments = new Dictionary<(AssignmentScope, uint), Guid>
        {
            [(AssignmentScope.Territory, 10)] = territoryProfile,
            [(AssignmentScope.Duty, 20)] = Guid.Empty
        };
        var defaultProfile = Guid.NewGuid();
        Assert.Equal(Guid.Empty, ProfileRules.Resolve(assignments, 10, 20, 0, true, false, defaultProfile));
        Assert.Equal(territoryProfile, ProfileRules.Resolve(assignments, 10, 0, 0, false, false, defaultProfile));
        Assert.Equal(defaultProfile, ProfileRules.Resolve(assignments, 11, 0, 0, false, false, defaultProfile));
    }

    [Fact]
    public void DutyGroupsRequireMatchingNameAndTerritory()
    {
        var first = new DutyGroupKey(10, "Trial");
        Assert.Equal(first, new DutyGroupKey(10, "Trial"));
        Assert.NotEqual(first, new DutyGroupKey(11, "Trial"));
        Assert.NotEqual(first, new DutyGroupKey(10, "Raid"));
    }

    [Fact]
    public void NormalizeAllowsOnlyOneProfilePerTarget()
    {
        var first = new CursorProfile { Name = "First" };
        var second = new CursorProfile { Name = "Second" };
        var profiles = new List<CursorProfile> { first, second };
        var assignments = new List<CursorAssignment>
        {
            new() { Scope = AssignmentScope.Territory, TargetId = 10, ProfileId = first.Id },
            new() { Scope = AssignmentScope.Territory, TargetId = 10, ProfileId = second.Id },
            new() { Scope = AssignmentScope.Duty, TargetId = 10, ProfileId = first.Id }
        };

        Assert.True(ProfileRules.Normalize(profiles, assignments));
        Assert.Equal(2, assignments.Count);
        Assert.Single(assignments, value => value.Scope == AssignmentScope.Territory && value.TargetId == 10);
        Assert.Single(assignments, value => value.Scope == AssignmentScope.Duty && value.TargetId == 10);
    }

    [Fact]
    public void ZoneVariantsNormalizeToOnePlaceTarget()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var assignments = new List<CursorAssignment>
        {
            new() { Scope = AssignmentScope.Territory, TargetId = 100, ProfileId = first },
            new() { Scope = AssignmentScope.Territory, TargetId = 101, ProfileId = second },
            new() { Scope = AssignmentScope.Territory, TargetId = 999, ProfileId = first },
            new() { Scope = AssignmentScope.Duty, TargetId = 101, ProfileId = first }
        };
        var groups = new Dictionary<uint, uint>
        {
            [100] = 100,
            [101] = 100
        };

        Assert.True(ProfileRules.NormalizeCatalogTargets(assignments, groups, new Dictionary<uint, uint>(), new Dictionary<uint, uint>(), new HashSet<uint>()));
        Assert.Equal(3, assignments.Count);
        Assert.Single(assignments, value => value.Scope == AssignmentScope.Territory && value.TargetId == 100);
        Assert.Single(assignments, value => value.Scope == AssignmentScope.Territory && value.TargetId == 999);
        Assert.Single(assignments, value => value.Scope == AssignmentScope.Duty && value.TargetId == 101);
    }

    [Fact]
    public void MissingDefaultProfileFallsBackToDefault()
    {
        var profile = new CursorProfile { Name = "Existing" };
        var profiles = new List<CursorProfile> { profile };

        Assert.Equal(profile.Id, ProfileRules.NormalizeDefaultProfileId(profiles, profile.Id));
        Assert.Equal(Guid.Empty, ProfileRules.NormalizeDefaultProfileId(profiles, Guid.NewGuid()));
        Assert.Equal(Guid.Empty, ProfileRules.NormalizeDefaultProfileId(profiles, Guid.Empty));
    }

    [Fact]
    public void ResolverSeparatesDomainsAndUsesSpecificBeforeAny()
    {
        var zone = Guid.NewGuid();
        var duty = Guid.NewGuid();
        var dutyAny = Guid.NewGuid();
        var pvp = Guid.NewGuid();
        var pvpAny = Guid.NewGuid();
        var fallback = Guid.NewGuid();
        var assignments = new Dictionary<(AssignmentScope, uint), Guid>
        {
            [(AssignmentScope.Territory, 10)] = zone,
            [(AssignmentScope.Duty, 20)] = duty,
            [(AssignmentScope.DutyAny, 0)] = dutyAny,
            [(AssignmentScope.PvP, 30)] = pvp,
            [(AssignmentScope.PvPAny, 0)] = pvpAny
        };

        Assert.Equal(zone, ProfileRules.Resolve(assignments, 10, 0, 0, false, false, fallback));
        Assert.Equal(duty, ProfileRules.Resolve(assignments, 10, 20, 0, true, false, fallback));
        Assert.Equal(dutyAny, ProfileRules.Resolve(assignments, 10, 21, 0, true, false, fallback));
        Assert.Equal(pvp, ProfileRules.Resolve(assignments, 10, 20, 30, true, true, fallback));
        Assert.Equal(pvpAny, ProfileRules.Resolve(assignments, 10, 20, 31, true, true, fallback));
    }

    [Fact]
    public void NormalizeAcceptsOnlyValidScopeTargets()
    {
        var profile = new CursorProfile { Name = "Profile" };
        var assignments = new List<CursorAssignment>
        {
            new() { Scope = AssignmentScope.Territory, TargetId = 0, ProfileId = profile.Id },
            new() { Scope = AssignmentScope.DutyAny, TargetId = 1, ProfileId = profile.Id },
            new() { Scope = AssignmentScope.PvPAny, TargetId = 0, ProfileId = profile.Id },
            new() { Scope = AssignmentScope.PvP, TargetId = 2, ProfileId = profile.Id }
        };

        Assert.True(ProfileRules.Normalize([profile], assignments));
        Assert.Equal(2, assignments.Count);
        Assert.Contains(assignments, value => value.Scope == AssignmentScope.PvPAny && value.TargetId == 0);
        Assert.Contains(assignments, value => value.Scope == AssignmentScope.PvP && value.TargetId == 2);
    }

    [Fact]
    public void CatalogNormalizationSeparatesAndMigratesTargets()
    {
        var assignments = new List<CursorAssignment>
        {
            new() { Scope = AssignmentScope.Territory, TargetId = 101 },
            new() { Scope = AssignmentScope.Territory, TargetId = 200 },
            new() { Scope = AssignmentScope.Territory, TargetId = 999 },
            new() { Scope = AssignmentScope.Duty, TargetId = 301 },
            new() { Scope = AssignmentScope.Duty, TargetId = 401 }
        };
        var zones = new Dictionary<uint, uint> { [101] = 100 };
        var duties = new Dictionary<uint, uint> { [301] = 300 };
        var pvpDuties = new Dictionary<uint, uint> { [401] = 400 };
        var contextual = new HashSet<uint> { 200 };

        Assert.True(ProfileRules.NormalizeCatalogTargets(assignments, zones, duties, pvpDuties, contextual));
        Assert.Equal(4, assignments.Count);
        Assert.Contains(assignments, value => value.Scope == AssignmentScope.Territory && value.TargetId == 100);
        Assert.Contains(assignments, value => value.Scope == AssignmentScope.Territory && value.TargetId == 999);
        Assert.Contains(assignments, value => value.Scope == AssignmentScope.Duty && value.TargetId == 300);
        Assert.Contains(assignments, value => value.Scope == AssignmentScope.PvP && value.TargetId == 400);
    }
}
