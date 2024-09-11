using System;
using System.Collections.Generic;
using System.Linq;
using cantorsdust.Common.Integrations;

using GenericModConfigMenu;

using StardewModdingAPI;
using StardewValley;

using TimeSpeed.Framework.Integrations.UiInfoSuite2;

namespace TimeSpeed.Framework;
/// <summary>Configures the integration with Generic Mod Config Menu.</summary>
internal static class GenericModConfigMenuIntegration
{
#region Private Static Properties
    /// <summary>Classes that need to disposed at the shutdown of the game.</summary>
    private static HashSet<IDisposable> Disposables => [ ];
#endregion

#region Public methods
    /// <summary>Add a config UI to Generic Mod Config Menu if it's installed.</summary>
    /// <param name="manifest">The mod manifest.</param>
    /// <param name="modRegistry">The mod registry from which to get the API.</param>
    /// <param name="modHelper">The helper with which helps set up the mod.</param>
    /// <param name="monitor">The monitor with which to log errors.</param>
    /// <param name="getConfig">Get the current mod configuration.</param>
    /// <param name="reset">Reset the config to its default values.</param>
    /// <param name="save">Save the current config to the <c>config.json</c> file.</param>
    /// <param name="titleScreenOnly">Whether the options can only be edited from the title screen.</param>
    public static void Register(IManifest manifest, IModRegistry modRegistry, IModHelper modHelper, IMonitor monitor, Func<ModConfig> getConfig, Action reset, Action save, bool titleScreenOnly = false)
    {
        // get API
        IGenericModConfigMenuApi? api = IntegrationHelper.GetGenericModConfigMenu(modRegistry, monitor);

        if (api is null)
            return;

        var _showTimeSpeedIcon  = new TimeSpeedIcon(modHelper);
        var _showTimePausedIcon = new TimePausedIcon(modHelper);

        GenericModConfigMenuIntegration.Disposables.Add(_showTimeSpeedIcon);
        GenericModConfigMenuIntegration.Disposables.Add(_showTimePausedIcon);
        _showTimeSpeedIcon.ToggleOption(getConfig().ShowTimeSpeedIcon && getConfig().UiInfoSuite2Integration);
        _showTimePausedIcon.ToggleOption(getConfig().ShowTimePausedIcon && getConfig().UiInfoSuite2Integration);

        // register config UI
        api.Register(manifest, reset: reset, save: save, titleScreenOnly: titleScreenOnly);

        // general options
        const float minSecondsPerMinute = 0.1f;
        const float maxSecondsPerMinute = 15f;

        // ReSharper disable BadParensLineBreaks
        // ReSharper disable MissingBlankLines
        api.OnFieldChanged(
            manifest,
            (name, value) => {
                switch(name) {
                    case "enableHostOnly" when value is bool bValue && ModEntry.MessageManager is not null:
                        ModEntry.MessageManager.SendTimeConfigStateMessage(Game1.player, hostOnly: bValue);
                        break;
                    case "enableClientVote" when value is bool cValue && ModEntry.MessageManager is not null:
                        ModEntry.MessageManager.SendTimeConfigStateMessage(Game1.player, voteEnabled: cValue);
                        break;
                    case "clientVoteThreshold" when value is float fValue && ModEntry.MessageManager is not null:
                        ModEntry.MessageManager.SendTimeConfigStateMessage(Game1.player, voteThreshold: fValue);
                        break;
                }
            }
        );

        api.AddSectionTitle(manifest, I18n.Config_GeneralOptions);
        api.AddBoolOption(
            manifest,
            name: I18n.Config_EnableOnFestivalDays_Name,
            tooltip: I18n.Config_EnableOnFestivalDays_Desc,
            getValue: () => getConfig().EnableOnFestivalDays,
            setValue: value => getConfig().EnableOnFestivalDays = value
        );
        api.AddBoolOption(
            manifest,
            name: I18n.Config_LocationNotify_Name,
            tooltip: I18n.Config_LocationNotify_Desc,
            getValue: () => getConfig().LocationNotify,
            setValue: value => getConfig().LocationNotify = value
        );

        api.AddSectionTitle(manifest, I18n.Config_Integrations);
        api.AddBoolOption(
            manifest,
            name: I18n.Config_UiInfoSuite2Integration_Name,
            tooltip: I18n.Config_UiInfoSuite2Integration_Desc,
            getValue: () => getConfig().UiInfoSuite2Integration,
            setValue: value => getConfig().UiInfoSuite2Integration = value
        );
        api.AddBoolOption(
            manifest,
            name: I18n.Config_UiInfoSuite2Integration_ShowTimeSpeedIcon_Name,
            tooltip: I18n.Config_UiInfoSuite2Integration_ShowTimeSpeedIcon_Desc,
            getValue: () => getConfig().ShowTimeSpeedIcon,
            setValue: value => {
                getConfig().ShowTimeSpeedIcon = value;
                _showTimeSpeedIcon.ToggleOption(value && getConfig().UiInfoSuite2Integration);
            }
        );
        api.AddBoolOption(
            manifest,
            name: I18n.Config_UiInfoSuite2Integration_ShowTimePausedIcon_Name,
            tooltip: I18n.Config_UiInfoSuite2Integration_ShowTimePausedIcon_Desc,
            getValue: () => getConfig().ShowTimePausedIcon,
            setValue: value => {
                getConfig().ShowTimePausedIcon = value;
                _showTimeSpeedIcon.ToggleOption(value && getConfig().UiInfoSuite2Integration);
            }
        );

        // seconds per minute section
        api.AddSectionTitle(manifest, I18n.Config_SecondsPerMinute);
        api.AddNumberOption(
            manifest,
            name: I18n.Config_IndoorsSpeed_Name,
            tooltip: I18n.Config_IndoorsSpeed_Desc,
            getValue: () => (float)getConfig().SecondsPerMinute!.Indoors,
            setValue: value => getConfig().SecondsPerMinute!.Indoors = Math.Round(value, 2),
            min: minSecondsPerMinute,
            max: maxSecondsPerMinute,
            interval: 0.1f
        );
        api.AddNumberOption(
            manifest,
            name: I18n.Config_OutdoorsSpeed_Name,
            tooltip: I18n.Config_OutdoorsSpeed_Desc,
            getValue: () => (float)getConfig().SecondsPerMinute!.Outdoors,
            setValue: value => getConfig().SecondsPerMinute!.Outdoors = Math.Round(value, 2),
            min: minSecondsPerMinute,
            max: maxSecondsPerMinute,
            interval: 0.1f
        );
        api.AddNumberOption(
            manifest,
            name: I18n.Config_MineSpeed_Name,
            tooltip: I18n.Config_MineSpeed_Desc,
            getValue: () => (float)getConfig().SecondsPerMinute!.Mines,
            setValue: value => getConfig().SecondsPerMinute!.Mines = Math.Round(value, 2),
            min: minSecondsPerMinute,
            max: maxSecondsPerMinute,
            interval: 0.1f
        );
        api.AddNumberOption(
            manifest,
            name: I18n.Config_SkullCavernSpeed_Name,
            tooltip: I18n.Config_SkullCavernSpeed_Desc,
            getValue: () => (float)getConfig().SecondsPerMinute!.SkullCavern,
            setValue: value => getConfig().SecondsPerMinute!.SkullCavern = Math.Round(value, 2),
            min: minSecondsPerMinute,
            max: maxSecondsPerMinute,
            interval: 0.1f
        );
        api.AddNumberOption(
            manifest,
            name: I18n.Config_VolcanoDungeonSpeed_Name,
            tooltip: I18n.Config_VolcanoDungeonSpeed_Desc,
            getValue: () => (float)getConfig().SecondsPerMinute!.VolcanoDungeon,
            setValue: value => getConfig().SecondsPerMinute!.VolcanoDungeon = Math.Round(value, 2),
            min: minSecondsPerMinute,
            max: maxSecondsPerMinute,
            interval: 0.1f
        );

        // freeze time
        api.AddSectionTitle(manifest, I18n.Config_FreezeTime);
        api.AddBoolOption(
            manifest,
            name: I18n.Config_EnableHostOnly_Name,
            tooltip: I18n.Config_EnableHostOnly_Desc,
            getValue: () => getConfig().FreezeTime!.HostOnly,
            setValue: value => getConfig().FreezeTime!.HostOnly = value,
            fieldId: "enableHostOnly"
        );
        api.AddBoolOption(
            manifest,
            name: I18n.Config_ClientVote_Name,
            tooltip: I18n.Config_ClientVote_Desc,
            getValue: () => getConfig().FreezeTime!.ClientVote,
            setValue: value => getConfig().FreezeTime!.ClientVote = value,
            fieldId: "enableClientVote"
        );
        api.AddNumberOption(
            manifest,
            name: I18n.Config_ClientVoteThreshold_Name,
            tooltip: I18n.Config_ClientVoteThreshold_Desc,
            getValue: () => (int)(getConfig().FreezeTime!.VoteThreshold * 100.0),
            setValue: value => getConfig().FreezeTime!.VoteThreshold = Math.Round(value / 100.0, 2),
            min: 0,
            max: 100,
            interval: 1,
            fieldId: "clientVoteThreshold"
        );
        api.AddNumberOption(
            manifest,
            name: I18n.Config_AnywhereAtTime_Name,
            tooltip: I18n.Config_AnywhereAtTime_Desc,
            getValue: () => getConfig().FreezeTime!.AnywhereAtTime ?? 2600,
            setValue: value => getConfig().FreezeTime!.AnywhereAtTime = value == 2600 ? null : value,
            min: 600,
            max: 2600
        );
        api.AddBoolOption(
            manifest,
            name: I18n.Config_FreezeTimeIndoors_Name,
            tooltip: I18n.Config_FreezeTimeIndoors_Desc,
            getValue: () => getConfig().FreezeTime!.Indoors,
            setValue: value => getConfig().FreezeTime!.Indoors = value
        );
        api.AddBoolOption(
            manifest,
            name: I18n.Config_FreezeTimeOutdoors_Name,
            tooltip: I18n.Config_FreezeTimeOutdoors_Desc,
            getValue: () => getConfig().FreezeTime!.Outdoors,
            setValue: value => getConfig().FreezeTime!.Outdoors = value
        );
        api.AddBoolOption(
            manifest,
            name: I18n.Config_FreezeTimeMine_Name,
            tooltip: I18n.Config_FreezeTimeMine_Desc,
            getValue: () => getConfig().FreezeTime!.Mines,
            setValue: value => getConfig().FreezeTime!.Mines = value
        );
        api.AddBoolOption(
            manifest,
            name: I18n.Config_FreezeTimeSkullCavern_Name,
            tooltip: I18n.Config_FreezeTimeSkullCavern_Desc,
            getValue: () => getConfig().FreezeTime!.SkullCavern,
            setValue: value => getConfig().FreezeTime!.SkullCavern = value
        );
        api.AddBoolOption(
            manifest,
            name: I18n.Config_FreezeTimeVolcanoDungeon_Name,
            tooltip: I18n.Config_FreezeTimeVolcanoDungeon_Desc,
            getValue: () => getConfig().FreezeTime!.VolcanoDungeon,
            setValue: value => getConfig().FreezeTime!.VolcanoDungeon = value
        );
        api.AddBoolOption(
            manifest,
            name: I18n.Config_FreezeTimeDuringEvents_Name,
            tooltip: I18n.Config_FreezeTimeDuringEvents_Desc,
            getValue: () => getConfig().FreezeTime!.DuringEvents,
            setValue: value => getConfig().FreezeTime!.DuringEvents = value
        );
        api.AddTextOption(
            manifest,
            name: I18n.Config_FreezeTimeFreezeNames_Name,
            tooltip: I18n.Config_FreezeTimeFreezeNames_Desc,
            getValue: () => string.Join(", ", getConfig().FreezeTime!.ByLocationName),
            setValue: value => getConfig().FreezeTime!.ByLocationName = new(value.Split(",")
                                                                                 .Select(p => p.Trim())
                                                                                 .Where(p => p != string.Empty))
        );
        api.AddTextOption(
            manifest,
            name: I18n.Config_FreezeTimeDontFreezeNames_Name,
            tooltip: I18n.Config_FreezeTimeDontFreezeNames_Desc,
            getValue: () => string.Join(", ", getConfig().FreezeTime!.ExceptLocationNames),
            setValue: value => getConfig().FreezeTime!.ExceptLocationNames = new(value.Split(",")
                                                                                      .Select(p => p.Trim())
                                                                                      .Where(p => p != string.Empty))
        );

        // controls
        api.AddSectionTitle(manifest, I18n.Config_Controls);
        api.AddKeybindList(
            manifest,
            name: I18n.Config_FreezeTimeKey_Name,
            tooltip: I18n.Config_FreezeTimeKey_Desc,
            getValue: () => getConfig().Keys!.FreezeTime,
            setValue: value => getConfig().Keys!.FreezeTime = value
        );
        api.AddKeybindList(
            manifest,
            name: I18n.Config_SlowTimeKey_Name,
            tooltip: I18n.Config_SlowTimeKey_Desc,
            getValue: () => getConfig().Keys!.IncreaseTickInterval,
            setValue: value => getConfig().Keys!.IncreaseTickInterval = value
        );
        api.AddKeybindList(
            manifest,
            name: I18n.Config_SpeedUpTimeKey_Name,
            tooltip: I18n.Config_SpeedUpTimeKey_Desc,
            getValue: () => getConfig().Keys!.DecreaseTickInterval,
            setValue: value => getConfig().Keys!.DecreaseTickInterval = value
        );
        api.AddKeybindList(
            manifest,
            name: I18n.Config_ReloadKey_Name,
            tooltip: I18n.Config_ReloadKey_Desc,
            getValue: () => getConfig().Keys!.ReloadConfig,
            setValue: value => getConfig().Keys!.ReloadConfig = value
        );
    }

    public static void Unload() {
        foreach (IDisposable disposable in GenericModConfigMenuIntegration.Disposables) {
            disposable.Dispose();
        }
    }
#endregion
}
