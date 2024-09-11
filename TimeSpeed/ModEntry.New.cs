using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Netcode;

using StardewValley;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using cantorsdust.Common.Extensions;

using TimeSpeed.Framework;
using TimeSpeed.Framework.Managers;
using TimeSpeed.Framework.Models.Enum;
using TimeSpeed.Framework.Models.Messages.Interfaces;

namespace TimeSpeed;
// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>The entry class called by SMAPI.</summary>
internal partial class ModEntry
{
#region Properties
    /// <summary>Displays messages to the user.</summary>
    private static ModEntry _instance = null!;

    /// <summary>(Local) Multiplayer mod message handler for allowing clients to pause time.</summary>
    private MessageManager? _messageManager;

    /// <summary>Multiplayer mod message handler for allowing clients to pause time.</summary>
    internal static MessageManager? MessageManager => ModEntry._instance?._messageManager;

    /// <summary>The mod monitor.</summary>
    internal static IMonitor IMonitor => ModEntry._instance.Monitor;

    // ReSharper disable once RedundantDefaultMemberInitializer
    /// <summary>The last notification sent via <see cref="ModEntry.UpdateSettingsForLocation"/>.</summary>
    private string? lastNotif { get; set; } = null;

    // ReSharper disable ArrangeObjectCreationWhenTypeEvident

    /// <summary>External property for host config option for config entry <see cref="ModFreezeTimeConfig.HostOnly"/>.</summary>
    private static readonly NetBool HostHostOnly = new(true);

    /// <summary>External property for host config option for config entry <see cref="ModFreezeTimeConfig.ClientVote"/>.</summary>
    private static readonly NetBool HostVoteEnabled = new(false);

    /// <summary>External property for host config option for config entry <see cref="ModFreezeTimeConfig.VoteThreshold"/>.</summary>
    private static readonly NetDouble HostVoteThreshold = new(1.0);

    // ReSharper restore ArrangeObjectCreationWhenTypeEvident

    /// <summary>Interval accessor to determine the game ticks. Default to 7 if null.</summary>
    internal static int BaseGameTickIntervalSeconds => Game1.currentLocation?.ExtraMillisecondsPerInGameMinute ?? 7;

    /// <summary>Internal accessor to determine the current game tick interval in seconds.</summary>
    internal static int TickIntervalSeconds => _instance.TickInterval / 1000;

    /// <summary>Returns <see langword="null"/> if no increase or decrease. If is increasing returns <see langword="true"/> otherwise <see langword="false"/>.</summary>
    internal static bool? IsIncrease => ModEntry.TickIntervalSeconds == ModEntry.BaseGameTickIntervalSeconds
        ? null
        : ModEntry.TickIntervalSeconds > ModEntry.BaseGameTickIntervalSeconds;
#endregion

#region Public methods
    /// <inheritdoc />
    public ModEntry() {
        ModEntry._instance = this;
    }
#endregion

#region Internal methods
    /// <summary>Gets a <see cref="GameLocation"/> from the location's <see cref="GameLocation.NameOrUniqueName"/>.</summary>
    /// <param name="location">The <see cref="GameLocation.NameOrUniqueName"/> of a <see cref="GameLocation"/>.</param>
    internal static GameLocation? GetLocationFromID(string? location)
    {
        return Game1.locations.FirstOrDefault(x => x.NameOrUniqueName == location);
    }

    /// <summary>Gets a <see cref="GameLocation"/>'s <see cref="GameLocation.NameOrUniqueName"/> as a byte array.</summary>
    /// <param name="location">The instance of a game location.</param>
    internal static string? GetIDFromLocation(GameLocation? location)
    {
        return location?.NameOrUniqueName;
    }

    /// <summary>Wrapper for handling inputs from a client or a host.</summary>
    /// <param name="freezeTimeMethod">The method of freezing time.</param>
    /// <param name="tickIntervalState">The state to change the tick interval to.</param>
    /// <param name="location">The location to pass from a client to the host.</param>
    internal static void HandleInput(FreezeTimeMethod freezeTimeMethod = FreezeTimeMethod.None, bool? tickIntervalState = null, GameLocation? location = null)
    {
        ModEntry._instance.HandleInputImpl(freezeTimeMethod, tickIntervalState, location, false, true);
    }

    /// <summary>Sets the local instanced host variables.</summary>
    /// <param name="hostOnly">The value of host's <see cref="ModFreezeTimeConfig.HostOnly"/> config option, <see langword="null"/> if unchanged.</param>
    /// <param name="voteEnabled">The value of host's <see cref="ModFreezeTimeConfig.ClientVote"/> config option, <see langword="null"/> if unchanged.</param>
    /// <param name="voteThreshold">The value of host's <see cref="ModFreezeTimeConfig.VoteThreshold"/> config option, <see langword="null"/> if unchanged.</param>
    private void SetHostConfigImpl(bool? hostOnly = null, bool? voteEnabled = null, double? voteThreshold = null)
    {
        if (hostOnly is not null)
        {
            this.Notifier.QuickNotify(I18n.Message_HostChanged_HostOnly(state: GetLocalizedOnOffState(hostOnly.Value)), false);
            ModEntry.HostHostOnly.Set(hostOnly.Value);
        }

        if (voteEnabled is not null)
        {
            this.Notifier.QuickNotify(I18n.Message_HostChanged_VoteEnabled(state: GetLocalizedOnOffState(voteEnabled.Value)), false);
            ModEntry.HostVoteEnabled.Set(voteEnabled.Value);
        }

        // ReSharper disable once InvertIf
        if (voteThreshold is not null)
        {
            this.Notifier.QuickNotify(I18n.Message_HostChanged_VoteThreshold(value: ((int)(voteThreshold.Value * 100)).ToString()), false);
            ModEntry.HostVoteThreshold.Set(voteThreshold.Value);
        }
    }

    /// <summary>Sets the local instanced host variables.</summary>
    /// <param name="hostOnly">The value of host's <see cref="ModFreezeTimeConfig.HostOnly"/> config option, <see langword="null"/> if unchanged.</param>
    /// <param name="voteEnabled">The value of host's <see cref="ModFreezeTimeConfig.ClientVote"/> config option, <see langword="null"/> if unchanged.</param>
    /// <param name="voteThreshold">The value of host's <see cref="ModFreezeTimeConfig.VoteThreshold"/> config option, <see langword="null"/> if unchanged.</param>
    internal static void SetHostConfig(bool? hostOnly = null, bool? voteEnabled = null, double? voteThreshold = null)
    {
        ModEntry._instance.SetHostConfigImpl(hostOnly, voteEnabled, voteThreshold);
    }
#endregion

#region Private methods
#region Event handlers
#region New
    /// <inheritdoc cref="IMultiplayerEvents.PeerConnected"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (this._messageManager is null || e.Peer.IsHost || !Context.IsMainPlayer)
            return;

        this._messageManager.SendTimeConfigStateMessage(
            farmer: Game1.MasterPlayer,
            hostOnly: this.Config.FreezeTime.HostOnly,
            voteEnabled: this.Config.FreezeTime.ClientVote,
            voteThreshold: this.Config.FreezeTime.VoteThreshold);
    }

    /// <inheritdoc cref="IMultiplayerEvents.ModMessageReceived"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != this.ModManifest.UniqueID || this._messageManager is null)
            return;

        this._messageManager.HandleIncomingMessage(e);
    }

    /// <inheritdoc cref="IGameLoopEvents.ReturnedToTitle"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        // Unload if the main player quits.
        if (Context.ScreenId != 0)
            return;

        GenericModConfigMenuIntegration.Unload();
    }
#endregion

#region Patched
    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        GenericModConfigMenuIntegration.Register(
            this.ModManifest,
            this.Helper.ModRegistry,
            this.Helper,
            this.Monitor,
            getConfig: () => this.Config,
            reset: () => this.Config = new(),
            save: () =>
            {
                this.Helper.WriteConfig(this.Config);

                if (Context.IsWorldReady && this.ShouldEnable())
                    this.UpdateSettingsForLocationForAllPlayers();
            });
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!ModEntry.HostVoteEnabled.Get() && !this.Config.FreezeTime.HostOnly && !Context.IsMainPlayer)
            this.Monitor.Log("Disabled mod; only works for the main player in multiplayer.", LogLevel.Warn);
    }

    /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!this.ShouldEnable())
            return;

        this.UpdateScaleForDay(Game1.season, Game1.dayOfMonth);
        this.UpdateTimeFreeze(clearPreviousOverrides: true);
        this.UpdateSettingsForLocationForAllPlayers();
    }

    /// <inheritdoc cref="IInputEvents.ButtonsChanged"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        bool? freezeTimeMethod = null;
        bool? reloadConfig     = null;
        bool? increase         = null;

        if (this.Config.Keys.FreezeTime.JustPressed())
            freezeTimeMethod = true;

        if (this.Config.Keys.ReloadConfig.JustPressed())
            reloadConfig = true;

        if (this.Config.Keys.IncreaseTickInterval.JustPressed())
            increase = true;

        if (this.Config.Keys.DecreaseTickInterval.JustPressed())
            increase = false;

        if (freezeTimeMethod is not null || reloadConfig is not null || increase is not null)
        {
            this.HandleInputImpl(freezeTimeMethod == true ? FreezeTimeMethod.Manual : FreezeTimeMethod.None, increase, null, reloadConfig ?? false);
        }
    }

    /// <inheritdoc cref="IPlayerEvents.Warped"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!this.ShouldEnable() || (this.Config.FreezeTime.HostOnly && !e.IsLocalPlayer))
            return;

        this.UpdateSettingsForLocation(e.NewLocation);
    }

    /// <inheritdoc cref="IGameLoopEvents.UpdateTicked"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!this.ShouldEnable())
            return;

        this.TimeHelper.Update();
    }
#endregion
#endregion

#region Methods
    /// <summary>A custom method to initialize new stuff from the entry point of the mod.</summary>
    /// <param name="helper">Pass the mod helper from the mod entry point.</param>
    private void InitializeNewStuff(IModHelper helper)
    {
        // Load managers
        this._messageManager                         =  new(this.Monitor, helper, this.Notifier, this.ModManifest.UniqueID);
        helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
        helper.Events.Multiplayer.PeerConnected      += this.OnPeerConnected;
        helper.Events.GameLoop.ReturnedToTitle       += this.OnReturnedToTitle;
        this._timer                                  =  new(this.Config.FreezeTime.ClientVoteTimeout, helper);
        this._timer.OnFinished                       += this.OnTimerFinished;
    }

    /// <summary>Parsed an exception and a optional message into a proper error log message.</summary>
    /// <param name="exception">An instance of an exception.</param>
    /// <param name="message">Optional message to print with the exception.</param>
    /// <returns>A proper error log message.</returns>
    private static string ParseLogError(Exception exception, string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            message = string.Empty;
        else
            message += "\n";

        message += exception.Message;

        if (exception.StackTrace.IsNullOrEmptyOrWhiteSpace())
            message += "\n" + exception.StackTrace;

        return message;
    }

    /// <summary>Log exceptions to console from anywhere in this code.</summary>
    /// <param name="exception">An instance of an exception.</param>
    /// <param name="message">Optional message to print with the exception.</param>
    /// <param name="logLevel">A custom log level for the logged message.</param>
    internal static void LogException(Exception exception, string? message = null, LogLevel logLevel = LogLevel.Error)
    {
        ModEntry._instance.Monitor.Log(ModEntry.ParseLogError(exception, message), logLevel);
    }

    /// <summary>Log an exception once to console from anywhere in this code.</summary>
    /// <param name="exception">An instance of an exception.</param>
    /// <param name="message">Optional message to print with the exception.</param>
    /// <param name="logLevel">A custom log level for the logged message.</param>
    internal static void LogExceptionOnce(Exception exception, string? message = null, LogLevel logLevel = LogLevel.Error)
    {
        ModEntry._instance.Monitor.LogOnce(ModEntry.ParseLogError(exception, message), logLevel);
    }

    /// <summary>Get the farmer from the mod message received.</summary>
    /// <param name="e">The event args from the mod message received</param>
    /// <returns>An instance of a farmer if found otherwise null.</returns>
    [SuppressMessage("CodeQuality", "IDE0079"), SuppressMessage("Major Code Smell", "S1144"), SuppressMessage("CodeQuality", "IDE0051"), SuppressMessage("ReSharper", "UnusedMember.Local")]
    private Farmer? GetFarmer(ModMessageReceivedEventArgs e) {
        ITimeMessage genericMessage;

        try {
            genericMessage = e.ReadAs<ITimeMessage>();
        } catch (Exception exception) {
            ModEntry.LogExceptionOnce(exception, "Failed to parse message as an ITimeMessage interface.");

            return null;
        }

        try {
            return Game1.getFarmer(genericMessage.FarmerID);
        } catch (Exception exception) {
            ModEntry.LogExceptionOnce(exception, $"Failed to get farmer with the id {genericMessage.FarmerID}.");
        }

        return null;
    }

    /// <summary>Get whether time features should be enabled.</summary>
    /// <param name="forInput">Whether to check for input handling.</param>
    /// <param name="fromNetwork">Whether to check when handling from the network.</param>
    private bool ShouldEnable(bool forInput = false, bool fromNetwork = false, bool do_debug = false)
    {
        if (do_debug) {
            this.Monitor.Log($" ", LogLevel.Info);
            this.Monitor.Log($"===========================================================================================", LogLevel.Info);
            this.Monitor.Log($" ", LogLevel.Info);
            this.Monitor.Log($"    !Context.IsWorldReady = {!Context.IsWorldReady}", LogLevel.Info);
            this.Monitor.Log($"    this.Config.FreezeTime.HostOnly = {this.Config.FreezeTime.HostOnly}", LogLevel.Info);
            this.Monitor.Log($"    fromNetwork = {fromNetwork}", LogLevel.Info);
            this.Monitor.Log($"    this.Config.FreezeTime.HostOnly && fromNetwork = {this.Config.FreezeTime.HostOnly && fromNetwork}", LogLevel.Info);
            this.Monitor.Log($"    !Context.IsWorldReady || (this.Config.FreezeTime.HostOnly && fromNetwork) = {!Context.IsWorldReady || (this.Config.FreezeTime.HostOnly && fromNetwork)}", LogLevel.Info);
        }

        // is loaded and host player (farmhands can't change time)
        if (!Context.IsWorldReady || (this.Config.FreezeTime.HostOnly && fromNetwork)) {
            if (do_debug) {
                this.Monitor.Log($" => ShouldEnable = {false}", LogLevel.Info);
            }

            return false;
        }

        if (do_debug) {
            this.Monitor.Log($"    !forInput = {!forInput}", LogLevel.Info);
            this.Monitor.Log($"    fromNetwork = {fromNetwork}", LogLevel.Info);
            this.Monitor.Log($"    !forInput && fromNetwork = {!forInput && fromNetwork}", LogLevel.Info);
        }

        // check restrictions for input
        if (!forInput || (Context.IsMainPlayer && fromNetwork)) {
            if (do_debug) {
                this.Monitor.Log($" => ShouldEnable = {true}", LogLevel.Info);
            }

            return true;
        }

        if (do_debug) {
            this.Monitor.Log($"    !Context.IsPlayerFree = {!Context.IsPlayerFree}", LogLevel.Info);
            this.Monitor.Log($"    !Game1.eventUp = {!Game1.eventUp}", LogLevel.Info);
            this.Monitor.Log($"    !Context.IsPlayerFree && !Game1.eventUp = {!Context.IsPlayerFree && !Game1.eventUp}", LogLevel.Info);
        }

        // don't handle input when player isn't free (except in events)
        if (!Context.IsPlayerFree && !Game1.eventUp) {
            if (do_debug) {
                this.Monitor.Log($" => ShouldEnable = {false}", LogLevel.Info);
            }

            return false;
        }

        if (do_debug) {
            this.Monitor.Log($"    Game1.keyboardDispatcher.Subscriber is not null = {Game1.keyboardDispatcher.Subscriber is not null}", LogLevel.Info);
        }

        // ignore input if a textbox is active
        if (Game1.keyboardDispatcher.Subscriber is not null) {
            if (do_debug) {
                this.Monitor.Log($" => ShouldEnable = {false}", LogLevel.Info);
            }

            return false;
        }

        if (do_debug) {
            this.Monitor.Log($" => ShouldEnable = {true}", LogLevel.Info);
        }

        return true;
    }

    /// <summary>Reload <see cref="Config"/> from the config file.</summary>
    private void ReloadConfig()
    {
        this.Config = this.Helper.ReadConfig<ModConfig>();
        this.UpdateScaleForDay(Game1.season, Game1.dayOfMonth);
        this.UpdateSettingsForLocationForAllPlayers();
        this.Notifier.ShortNotify(I18n.Message_ConfigReloaded(), false);
    }

    /// <summary>Update the time settings for every player's location.</summary>
    private void UpdateSettingsForLocationForAllPlayers()
    {
        if (this.Config.FreezeTime.HostOnly)
        {
            this.UpdateSettingsForLocation(Game1.currentLocation);

            return;
        }

        foreach (Farmer player in Game1.getAllFarmers()) {
            this.UpdateSettingsForLocation(player.currentLocation);
        }
    }

    /// <summary>Update the time settings for the given location.</summary>
    /// <param name="location">The game location.</param>
    private void UpdateSettingsForLocation(GameLocation? location)
    {
        if (location is null)
            return;

        // update time settings
        this.UpdateTimeFreeze(location);
        this.TickInterval = this.Config.GetMillisecondsPerMinute(location) * 10;

        // notify player
        if (!this.Config.LocationNotify)
            return;

        string notif = this.AutoFreeze switch {
            AutoFreezeReason.FrozenAtTime when this._isTimeFrozen => I18n.Message_OnLocationChange_TimeStoppedGlobally(),
            AutoFreezeReason.FrozenForLocation when this._isTimeFrozen => I18n.Message_OnLocationChange_TimeStoppedHere(),
            AutoFreezeReason.FrozenDuringEvent when this._isTimeFrozen => I18n.Message_OnLocationChange_TimeStoppedDuringEvent(
                hand: Context.IsMainPlayer
                          ? I18n.Message_OnLocationChange_TimeStoppedDuringEvent_Farmer()
                          : I18n.Message_OnLocationChange_TimeStoppedDuringEvent_FarmHand(),
                farmer: Game1.player.Name),
            _ => I18n.Message_OnLocationChange_TimeSpeedHere(seconds: this.TickInterval / 1000),
        };

        if (this.lastNotif?.Equals(notif, StringComparison.OrdinalIgnoreCase) == true)
            return;

        this.Notifier.ShortNotify(notif, true);
        this.lastNotif = notif;
    }

    /// <summary>Update the <see cref="AutoFreeze"/> and <see cref="ManualFreeze"/> flags based on the current context.</summary>
    /// <param name="location">The game location.</param>
    /// <param name="manualOverride">An explicit freeze (<c>true</c>) or unfreeze (<c>false</c>) requested by the player, if applicable.</param>
    /// <param name="clearPreviousOverrides">Whether to clear any previous explicit overrides.</param>
    private void UpdateTimeFreeze(GameLocation? location = null, bool? manualOverride = null, bool clearPreviousOverrides = false)
    {
        bool? wasManualFreeze = this.ManualFreeze;
        AutoFreezeReason wasAutoFreeze = this.AutoFreeze;

        // update auto freeze
        this.AutoFreeze = this.GetAutoFreezeType(location);

        // update manual freeze
        if (manualOverride.HasValue)
            this.ManualFreeze = manualOverride.Value;
        else if (clearPreviousOverrides)
            this.ManualFreeze = null;

        // clear manual unfreeze if it's no longer needed
        if (this.ManualFreeze == false && this.AutoFreeze == AutoFreezeReason.None)
            this.ManualFreeze = null;

        // log change
        if (wasAutoFreeze != this.AutoFreeze)
        {
            this.Monitor.Log($"Auto freeze changed from {wasAutoFreeze} to {this.AutoFreeze}.");
        }

        if (wasManualFreeze != this.ManualFreeze)
        {
            this.Monitor.Log($"Manual freeze changed from {wasManualFreeze?.ToString() ?? "null"} to {this.ManualFreeze?.ToString() ?? "null"}.");
        }
    }

    /// <summary>Update the time settings for the given date.</summary>
    /// <param name="season">The current season.</param>
    /// <param name="dayOfMonth">The current day of month.</param>
    private void UpdateScaleForDay(Season season, int dayOfMonth)
    {
        this.AdjustTime = this.Config.ShouldScale(season, dayOfMonth);
    }

    /// <summary>Get the adjusted progress towards the next 10-game-minute tick.</summary>
    /// <param name="progress">The current progress.</param>
    /// <param name="newTickInterval">The new tick interval.</param>
    private double ScaleTickProgress(double progress, int newTickInterval)
    {
        return progress * TimeHelper.CurrentDefaultTickInterval / newTickInterval;
    }

    /// <summary>Get the freeze type which applies for the current context, ignoring overrides by the player.</summary>
    /// <param name="location">The game location.</param>
    private AutoFreezeReason GetAutoFreezeType(GameLocation? location = null)
    {
        if (location is not null && this.Config.ShouldFreeze(location))
            return AutoFreezeReason.FrozenForLocation;

        if (this.Config.ShouldFreeze(Game1.timeOfDay))
            return AutoFreezeReason.FrozenAtTime;

        if (!this.Config.FreezeTime.DuringEvents || !Game1.eventUp) return AutoFreezeReason.None;

        this._messageManager?.SendTimeManipulateMessage(farmer: Game1.player, freezeTimeMethod: FreezeTimeMethod.Event);

        return AutoFreezeReason.FrozenDuringEvent;
    }

    /// <summary>Tests if the <paramref name="farmerID"/> is the main farmer.</summary>
    /// <param name="farmerID">The <see cref="Farmer.UniqueMultiplayerID"/> of a farmer.</param>
    [SuppressMessage("CodeQuality", "IDE0079"), SuppressMessage("CodeQuality", "IDE0051"), SuppressMessage("ReSharper", "UnusedMember.Local")]
    private static bool IsMainFarmer(long farmerID)
    {
        return Game1.getFarmer(farmerID).IsMainPlayer;
    }

    /// <summary>Get a translation equivalent to "on" of "off".</summary>
    private static string GetLocalizedOnOffState(bool value)
    {
        return value ? I18n.Message_OnState() : I18n.Message_OffState();
    }

    /// <summary>Wrapper for handling inputs from a client or a host.</summary>
    /// <param name="freezeTimeMethod">The method of freezing time.</param>
    /// <param name="increase">The state to change the tick interval to.</param>
    /// <param name="location">The location to pass from a client to the host.</param>
    /// <param name="reloadConfig">Whether to reload the config, applicable to the host only.</param>
    /// <param name="fromNetwork">Whether handle network requests.</param>
    private void HandleInputImpl(
        FreezeTimeMethod freezeTimeMethod = FreezeTimeMethod.None,
        bool? increase = null,
        GameLocation? location = null,
        bool reloadConfig = false,
        bool fromNetwork = false)
    {
        if (!this.ShouldEnable(do_debug: false, fromNetwork: fromNetwork))
            return;

        if (!Context.IsMainPlayer)
        {
            if (!this.ShouldEnable(forInput: true))
                return;

            if (increase is not null)
            {
                this._messageManager?.SendTimeManipulateMessage(
                    farmer: Game1.player,
                    increase: increase);
                return;
            }

            // ReSharper disable once InvertIf
            if (freezeTimeMethod is FreezeTimeMethod.Manual)
            {
                this._messageManager?.SendTimeManipulateMessage(
                    farmer: Game1.player,
                    freezeTimeMethod: freezeTimeMethod,
                    location: location);
                return;
            }

            return;
        }

        if (!this.ShouldEnable(forInput: true, fromNetwork: fromNetwork, do_debug: false))
            return;

        if (freezeTimeMethod == FreezeTimeMethod.Manual)
            this.ToggleFreeze();
        else if (increase is not null)
            this.ChangeTickInterval(increase.Value);
        else if (reloadConfig)
            this.ReloadConfig();
    }
#endregion
#endregion
}
