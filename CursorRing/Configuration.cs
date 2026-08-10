using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace CursorRing;

[Serializable]
public sealed class Configuration : CursorSettings, IPluginConfiguration
{
    public List<CursorProfile> Profiles { get; set; } = [];
    public List<CursorAssignment> Assignments { get; set; } = [];
    public Guid DefaultProfileId { get; set; }

    public new bool Normalize()
    {
        var changed = base.Normalize();
        Profiles ??= [];
        Assignments ??= [];
        changed |= ProfileRules.Normalize(Profiles, Assignments);
        var defaultProfileId = ProfileRules.NormalizeDefaultProfileId(Profiles, DefaultProfileId);
        if (DefaultProfileId != defaultProfileId)
        {
            DefaultProfileId = defaultProfileId;
            changed = true;
        }
        return changed;
    }

    internal void Save()
    {
        Normalize();
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
