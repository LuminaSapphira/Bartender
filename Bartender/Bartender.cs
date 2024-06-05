using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using ImGuiNET;
using Bartender.UI;
using Newtonsoft.Json.Linq;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using System.Text.RegularExpressions;
using static Bartender.ProfileConfig;
using FFXIVClientStructs.FFXIV.Client.UI;
using Bartender.UI.Utils;
using Dalamud.Interface.Windowing;

namespace Bartender;

public unsafe class Bartender : IDalamudPlugin
{
    public const int NUM_OF_BARS = 10;
    public const int NUM_OF_SLOTS = 12;

    public static Bartender? Plugin { get; private set; }
    public static Configuration? Configuration { get; private set; }
    public static IconManager? IconManager { get; private set; }
    public static Game? Game { get; private set; }

    public BartenderUI UI;
    private bool isPluginReady = false;

    public readonly WindowSystem WindowSystem = new("Bartender");

    public static RaptureHotbarModule* RaptureHotbar { get; private set; } = Framework.Instance()->UIModule->GetRaptureHotbarModule();

    public Bartender(DalamudPluginInterface pluginInterface)
    {
        Plugin = this;
        DalamudApi.Initialize(this, pluginInterface);

        IpcProvider.Init();

        Configuration = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new();
        Configuration.Initialize();
        Configuration.UpdateVersion();

        IconManager = new();
        Game = new();

        DalamudApi.Framework.Update += Update;

        UI = new BartenderUI() {
#if DEBUG
            IsOpen = true,
#endif
        };

        WindowSystem.AddWindow(UI);

        DalamudApi.PluginInterface.UiBuilder.Draw += Draw;
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfig;

        ReadyPlugin();
    }

    public void ReadyPlugin()
    {
        try
        {
            Game.Initialize();
            ConditionManager.Initialize();

            isPluginReady = true;
        }
        catch (Exception e)
        {
            DalamudApi.PluginLog.Error($"Failed to load Bartender.\n{e}");
        }
    }

    public void Reload()
    {
        Configuration = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new();
        Configuration.Initialize();
        Configuration.UpdateVersion();
        Configuration.Save();
        UI.Reload();
    }

    public void ToggleConfig() => UI.Toggle();

    #region Commands
    [Command("/bartender")]
    [HelpMessage("Open the configuration menu.")]
    public void ToggleConfig(string command, string arguments) => ToggleConfig();

    [Command("/barload")]
    [HelpMessage("Load a bar profile (meant for macros). Usage: /barload <profile name>")]
    public void BarLoad(string command, string arguments)
    {
        if (arguments.IsNullOrEmpty())
            DalamudApi.ChatGui.PrintError("Wrong arguments. Usage: /barload <profile name>");

        ProfileConfig? profile = Configuration!.ProfileConfigs.Find(profile => profile.Name == arguments);
        if (profile == null)
            DalamudApi.ChatGui.PrintError($"The profile '{arguments}' does not exist.");
        BarControl(profile!, false);
    }

    [Command("/barclear")]
    [HelpMessage("Clears a bar profile (meant for macros). Usage: /barclear <profile name>")]
    public void BarClear(string command, string arguments)
    {
        if (arguments.IsNullOrEmpty())
            DalamudApi.ChatGui.PrintError("Wrong arguments. Usage: /barclear <profile name>");

        ProfileConfig? profile = Configuration!.ProfileConfigs.Find(profile => profile.Name == arguments);
        if (profile == null)
            DalamudApi.ChatGui.PrintError($"The profile '{arguments}' does not exist.");
        BarControl(profile!, true);
    }

    private void BarControl(ProfileConfig profile, bool clear)
    {
        for (int i = 0; i < 10; i++)
        {
            BarNums flag = (BarNums)(1 << i);
            if ((profile.UsedBars & flag) != flag)
                continue;

            HotbarSlot[] slots = profile.GetRow(i);
            for (uint slot = 0; slot < NUM_OF_SLOTS; slot++)
            {
                HotBarSlot* gameSlot = RaptureHotbar->GetSlotById(Convert.ToUInt32(i), slot);
                if (clear)
                    gameSlot->Set(HotbarSlotType.Empty, 0);
                else
                    gameSlot->Set(slots[slot].CommandType, slots[slot].CommandId);
                RaptureHotbar->WriteSavedSlot(RaptureHotbar->ActiveHotbarClassJobId, Convert.ToUInt32(i), slot, gameSlot, false, DalamudApi.ClientState.IsPvP);
            }
        }
    }
    #endregion

    public static float RunTime => (float)DalamudApi.PluginInterface.LoadTimeDelta.TotalSeconds;
    public static long FrameCount => (long)DalamudApi.PluginInterface.UiBuilder.FrameCount;

    private void Update(IFramework framework)
    {
        if (!isPluginReady) return;

        //ConditionManager.UpdateCache();
    }

    private void Draw()
    {
        if (!isPluginReady) return;

        WindowSystem.Draw();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        IpcProvider.DeInit();

        Configuration!.Save();

        DalamudApi.Framework.Update -= Update;
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfig;

        DalamudApi.Dispose();

        IconManager!.Dispose();
        UI.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
