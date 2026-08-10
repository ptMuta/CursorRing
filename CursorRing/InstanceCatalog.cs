using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace CursorRing;

internal readonly record struct InstanceCatalogEntry(uint DutyGroupId, string DutyName, string TerritoryName);
internal readonly record struct ZoneCatalogEntry(uint ZoneId, string Name, string SearchText);

internal sealed class InstanceCatalog
{
    private readonly Dictionary<uint, uint> dutyGroupsByDuty = [];
    private readonly Dictionary<uint, uint> zoneGroupsByTerritory = [];
    private readonly Dictionary<uint, ZoneCatalogEntry> zones = [];

    internal InstanceCatalog(IDataManager dataManager)
    {
        var zoneGroupsByPlace = new Dictionary<uint, uint>();
        foreach (var row in dataManager.GetExcelSheet<TerritoryType>())
        {
            if (row.RowId == 0 || row.PlaceName.RowId == 0)
            {
                continue;
            }
            var placeNameId = row.PlaceName.RowId;
            var name = row.PlaceName.Value.Name.ToString().Trim();
            if (name.Length == 0)
            {
                continue;
            }
            if (!zoneGroupsByPlace.TryGetValue(placeNameId, out var groupId))
            {
                groupId = row.RowId;
                zoneGroupsByPlace.Add(placeNameId, groupId);
                zones.Add(groupId, new ZoneCatalogEntry(groupId, name, name));
            }
            zoneGroupsByTerritory.Add(row.RowId, groupId);
        }
        var groups = new Dictionary<DutyGroupKey, uint>();
        foreach (var row in dataManager.GetExcelSheet<ContentFinderCondition>())
        {
            var name = row.Name.ToString().Trim();
            var territoryId = row.TerritoryType.RowId;
            if (name.Length == 0 || territoryId == 0)
            {
                continue;
            }
            var key = new DutyGroupKey(territoryId, name);
            if (!groups.TryGetValue(key, out var groupId))
            {
                groupId = row.RowId;
                groups.Add(key, groupId);
                var territoryName = row.TerritoryType.Value.PlaceName.Value.Name.ToString().Trim();
                Duties.Add(new InstanceCatalogEntry(groupId, name, territoryName.Length == 0 ? name : territoryName));
            }
            dutyGroupsByDuty.Add(row.RowId, groupId);
            var zoneGroupId = GetZoneGroup(territoryId);
            if (zones.TryGetValue(zoneGroupId, out var zone))
            {
                zones[zoneGroupId] = zone with { SearchText = zone.SearchText + '\n' + name };
            }
        }
        Duties.Sort(static (left, right) => string.Compare(left.DutyName, right.DutyName, StringComparison.OrdinalIgnoreCase));
        Zones = [.. zones.Values];
        Zones.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
    }

    internal List<InstanceCatalogEntry> Duties { get; } = [];
    internal List<ZoneCatalogEntry> Zones { get; }

    internal uint GetDutyGroup(uint dutyId)
    {
        return dutyGroupsByDuty.TryGetValue(dutyId, out var groupId) ? groupId : 0;
    }

    internal uint GetZoneGroup(uint territoryId)
    {
        return zoneGroupsByTerritory.TryGetValue(territoryId, out var groupId) ? groupId : 0;
    }

    internal bool NormalizeAssignments(List<CursorAssignment> assignments)
    {
        return ProfileRules.NormalizeZoneTargets(assignments, zoneGroupsByTerritory);
    }

    internal string GetZoneName(uint zoneId)
    {
        var groupId = GetZoneGroup(zoneId);
        return zones.TryGetValue(groupId == 0 ? zoneId : groupId, out var zone) ? zone.Name : $"Unknown ({zoneId})";
    }
}
