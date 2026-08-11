#if CURSORRING_BENCHMARK
using System;
using System.Diagnostics;
#endif
using CursorRing.Windows;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace CursorRing;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/cursorring";
#if CURSORRING_BENCHMARK
    private const string CommandHelp = "Open CursorRing settings. Use /cursorring benchmark to run a performance benchmark.";
#else
    private const string CommandHelp = "Open CursorRing settings.";
#endif

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDutyState DutyState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IAddonEventManager AddonEventManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private readonly WindowSystem windowSystem = new("CursorRing");
    private readonly ConfigWindow configWindow;
    private readonly CursorRenderer renderer;
    private readonly ProfileManager profiles;
    private readonly InstanceCatalog catalog;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        if (Configuration.Normalize())
        {
            Configuration.Save();
        }

        catalog = new InstanceCatalog(DataManager);
        if (catalog.NormalizeAssignments(Configuration.Assignments))
        {
            Configuration.Save();
        }
        profiles = new ProfileManager(Configuration);
        configWindow = new ConfigWindow(Configuration, profiles, catalog);
        renderer = new CursorRenderer(profiles, Condition, ClientState, PlayerState, AddonEventManager, PluginInterface.UiBuilder, Log);
        var dutyId = DutyState.ContentFinderCondition.RowId;
        ResolveProfile(ClientState.TerritoryType, dutyId, catalog.IsPvP(ClientState.TerritoryType, dutyId, ClientState.IsPvP));
        profiles.OnActiveChanged += renderer.ResetState;
        windowSystem.AddWindow(configWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = CommandHelp
        });

        PluginInterface.UiBuilder.Draw += OnDraw;
        PluginInterface.UiBuilder.HideUi += renderer.Hide;
        PluginInterface.UiBuilder.OpenMainUi += configWindow.Toggle;
        PluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
        ClientState.Logout += OnLogout;
        ClientState.ZoneInit += OnZoneInit;
        ClientState.EnterPvP += OnEnterPvP;
        ClientState.LeavePvP += OnLeavePvP;
    }

    internal Configuration Configuration { get; }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= OnDraw;
        PluginInterface.UiBuilder.HideUi -= renderer.Hide;
        PluginInterface.UiBuilder.OpenMainUi -= configWindow.Toggle;
        PluginInterface.UiBuilder.OpenConfigUi -= configWindow.Toggle;
        ClientState.Logout -= OnLogout;
        ClientState.ZoneInit -= OnZoneInit;
        ClientState.EnterPvP -= OnEnterPvP;
        ClientState.LeavePvP -= OnLeavePvP;
        profiles.OnActiveChanged -= renderer.ResetState;
        CommandManager.RemoveHandler(CommandName);
        renderer.ResetState();
        windowSystem.RemoveAllWindows();
    }

    private void OnCommand(string command, string arguments)
    {
#if CURSORRING_BENCHMARK
        if (string.Equals(arguments.Trim(), "benchmark", StringComparison.OrdinalIgnoreCase))
        {
            configWindow.Benchmark.Start();
            configWindow.IsOpen = true;
            return;
        }
#endif

        configWindow.Toggle();
    }

    private void OnDraw()
    {
#if CURSORRING_BENCHMARK
        if (configWindow.Benchmark.IsActive)
        {
            configWindow.Benchmark.Update(Stopwatch.GetTimestamp());
            if (configWindow.Benchmark.IsCollecting)
            {
                var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                var startedAt = Stopwatch.GetTimestamp();
                var work = renderer.DrawMeasured();
                var finishedAt = Stopwatch.GetTimestamp();
                var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                var result = configWindow.Benchmark.Record(finishedAt, finishedAt - startedAt, allocatedBytes, work);
                if (result is { } completed)
                {
                    Log.Information(completed.Format());
                }
            }
            else
            {
                renderer.Draw();
            }
        }
        else
        {
            renderer.Draw();
        }
#else
        renderer.Draw();
#endif

        windowSystem.Draw();
    }

    private void OnLogout(int type, int code)
    {
        renderer.ResetState();
        renderer.SetInDuty(false);
        profiles.Resolve(0, 0, 0, false, false);
    }

    private void OnZoneInit(Dalamud.Game.ClientState.ZoneInitEventArgs args)
    {
        renderer.ResetState();
        var territoryId = args.TerritoryType.RowId;
        var dutyId = args.ContentFinderCondition.RowId;
        ResolveProfile(territoryId, dutyId, catalog.IsPvP(territoryId, dutyId, ClientState.IsPvP));
    }

    private void OnEnterPvP()
    {
        renderer.ResetState();
        ResolveProfile(ClientState.TerritoryType, DutyState.ContentFinderCondition.RowId, true);
    }

    private void OnLeavePvP()
    {
        renderer.ResetState();
        ResolveProfile(ClientState.TerritoryType, DutyState.ContentFinderCondition.RowId, false);
    }

    private void ResolveProfile(uint territoryId, uint dutyId, bool inPvP)
    {
        var inDuty = dutyId != 0;
        renderer.SetInDuty(inDuty);
        profiles.Resolve(catalog.GetZoneGroup(territoryId), catalog.GetDutyGroup(dutyId), catalog.GetPvpGroup(territoryId), inDuty, inPvP);
    }
}
