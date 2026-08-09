using System;
using Dalamud.Configuration;

namespace CursorRing;

[Serializable]
public sealed class Configuration : CursorSettings, IPluginConfiguration
{
    internal void Save()
    {
        Normalize();
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
