using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace CursorRing;

internal readonly record struct InstanceCatalogEntry(uint DutyGroupId, string DutyName, string TerritoryName);
internal readonly record struct ZoneCatalogEntry(uint ZoneId, string Name, string SearchText);
internal readonly record struct PvpCatalogEntry(uint PvpGroupId, string Name, string SearchText);

internal sealed class InstanceCatalog
{
    private readonly Dictionary<uint, uint> dutyGroupsByDuty = [];
    private readonly Dictionary<uint, uint> pvpGroupsByDuty = [];
    private readonly Dictionary<uint, uint> pvpGroupsByTerritory = [];
    private readonly Dictionary<uint, uint> zoneGroupsByTerritory = [];
    private readonly HashSet<uint> contextualTerritories = [];
    private readonly Dictionary<uint, ZoneCatalogEntry> zones = [];
    private readonly Dictionary<uint, PvpCatalogEntry> pvp = [];

    internal InstanceCatalog(IDataManager dataManager)
    {
        var dutyTerritories = new HashSet<uint>();
        var pvpTerritories = new HashSet<uint>();
        var pvpTerritoriesByDuty = new Dictionary<uint, uint>();
        var dutyGroups = new Dictionary<DutyGroupKey, uint>();
        foreach (var row in dataManager.GetExcelSheet<ContentFinderCondition>())
        {
            var name = row.Name.ToString().Trim();
            var territoryId = row.TerritoryType.RowId;
            if (name.Length == 0 || territoryId == 0)
            {
                continue;
            }
            dutyTerritories.Add(territoryId);
            contextualTerritories.Add(territoryId);
            if (row.PvP)
            {
                pvpTerritories.Add(territoryId);
                pvpTerritoriesByDuty[row.RowId] = territoryId;
                continue;
            }
            var key = new DutyGroupKey(territoryId, name);
            if (!dutyGroups.TryGetValue(key, out var groupId))
            {
                groupId = row.RowId;
                dutyGroups.Add(key, groupId);
                var territoryName = row.TerritoryType.Value.PlaceName.Value.Name.ToString().Trim();
                Duties.Add(new InstanceCatalogEntry(groupId, name, territoryName.Length == 0 ? name : territoryName));
            }
            dutyGroupsByDuty[row.RowId] = groupId;
        }

        var zoneGroupsByPlace = new Dictionary<uint, uint>();
        var pvpGroupsByPlace = new Dictionary<uint, uint>();
        foreach (var row in dataManager.GetExcelSheet<TerritoryType>())
        {
            if (row.RowId == 0)
            {
                continue;
            }
            var isPvp = row.IsPvpZone || pvpTerritories.Contains(row.RowId);
            if (isPvp)
            {
                contextualTerritories.Add(row.RowId);
            }
            if (row.PlaceName.RowId == 0)
            {
                continue;
            }
            var name = row.PlaceName.Value.Name.ToString().Trim();
            if (name.Length == 0)
            {
                continue;
            }
            if (isPvp)
            {
                var groupId = GetOrAddPlaceGroup(pvpGroupsByPlace, row.PlaceName.RowId, row.RowId);
                pvpGroupsByTerritory[row.RowId] = groupId;
                if (!pvp.ContainsKey(groupId))
                {
                    pvp[groupId] = new PvpCatalogEntry(groupId, name, name);
                }
            }
            else if (!dutyTerritories.Contains(row.RowId))
            {
                var groupId = GetOrAddPlaceGroup(zoneGroupsByPlace, row.PlaceName.RowId, row.RowId);
                zoneGroupsByTerritory[row.RowId] = groupId;
                if (!zones.ContainsKey(groupId))
                {
                    zones[groupId] = new ZoneCatalogEntry(groupId, name, name);
                }
            }
        }

        foreach (var row in dataManager.GetExcelSheet<ContentFinderCondition>())
        {
            if (!pvpTerritoriesByDuty.TryGetValue(row.RowId, out var territoryId) || !pvpGroupsByTerritory.TryGetValue(territoryId, out var groupId))
            {
                continue;
            }
            pvpGroupsByDuty[row.RowId] = groupId;
            var name = row.Name.ToString().Trim();
            if (name.Length != 0 && pvp.TryGetValue(groupId, out var entry) && !entry.SearchText.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                pvp[groupId] = entry with { SearchText = entry.SearchText + '\n' + name };
            }
        }

        Duties.Sort(static (left, right) => string.Compare(left.DutyName, right.DutyName, StringComparison.OrdinalIgnoreCase));
        Zones = [.. zones.Values];
        Zones.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
        Pvp = [.. pvp.Values];
        Pvp.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
    }

    internal List<InstanceCatalogEntry> Duties { get; } = [];
    internal List<ZoneCatalogEntry> Zones { get; }
    internal List<PvpCatalogEntry> Pvp { get; }

    internal uint GetDutyGroup(uint dutyId) => dutyGroupsByDuty.TryGetValue(dutyId, out var groupId) ? groupId : 0;

    internal uint GetPvpGroup(uint territoryId) => pvpGroupsByTerritory.TryGetValue(territoryId, out var groupId) ? groupId : 0;

    internal uint GetZoneGroup(uint territoryId) => zoneGroupsByTerritory.TryGetValue(territoryId, out var groupId) ? groupId : 0;

    internal bool IsPvP(uint territoryId, uint dutyId, bool clientState)
    {
        return clientState || pvpGroupsByTerritory.ContainsKey(territoryId) || pvpGroupsByDuty.ContainsKey(dutyId);
    }

    internal bool NormalizeAssignments(List<CursorAssignment> assignments)
    {
        return ProfileRules.NormalizeCatalogTargets(assignments, zoneGroupsByTerritory, dutyGroupsByDuty, pvpGroupsByDuty, contextualTerritories);
    }

    internal string GetZoneName(uint zoneId) => zones.TryGetValue(zoneId, out var zone) ? zone.Name : $"Unknown ({zoneId})";

    internal string GetPvpName(uint pvpId) => pvp.TryGetValue(pvpId, out var entry) ? entry.Name : $"Unknown ({pvpId})";

    private static uint GetOrAddPlaceGroup(Dictionary<uint, uint> groups, uint placeId, uint territoryId)
    {
        if (!groups.TryGetValue(placeId, out var groupId))
        {
            groupId = territoryId;
            groups.Add(placeId, groupId);
        }
        return groupId;
    }

}
