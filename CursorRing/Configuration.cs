using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace CursorRing;

[Serializable]
public sealed class Configuration : CursorSettings, IPluginConfiguration
{
    public List<CursorProfile> Profiles { get; set; } = [];
    public List<CursorAssignment> Assignments { get; set; } = [];

    public new bool Normalize()
    {
        var changed = base.Normalize();
        Profiles ??= [];
        Assignments ??= [];
        return ProfileRules.Normalize(Profiles, Assignments) || changed;
    }

    internal void Save()
    {
        Normalize();
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
