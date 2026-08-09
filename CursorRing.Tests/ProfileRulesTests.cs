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
        var territories = new Dictionary<uint, Guid>
        {
            [10] = territoryProfile
        };
        var duties = new Dictionary<uint, Guid>
        {
            [20] = Guid.Empty
        };
        var defaultProfile = Guid.NewGuid();
        Assert.Equal(Guid.Empty, ProfileRules.Resolve(territories, duties, 10, 20, defaultProfile));
        Assert.Equal(territoryProfile, ProfileRules.Resolve(territories, duties, 10, 21, defaultProfile));
        Assert.Equal(defaultProfile, ProfileRules.Resolve(territories, duties, 11, 21, defaultProfile));
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

        Assert.True(ProfileRules.NormalizeZoneTargets(assignments, groups));
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
}
