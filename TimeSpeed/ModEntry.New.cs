using System;
using System.Linq;
using cantorsdust.Common.Extensions;

using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using TimeSpeed.Framework;
using TimeSpeed.Framework.Managers;
using TimeSpeed.Framework.Models.Enum;
using TimeSpeed.Framework.Models.Messages.Interfaces;

#nullable enable

namespace TimeSpeed
{
    internal partial class ModEntry : Mod
    {
        /*********
        ** Properties
        *********/
#region Properties
        /// <summary>Displays messages to the user.</summary>
        private static ModEntry _instance = null!;

        /// <summary>(Local) Multiplayer mod message handler for allowing clients to pause time.</summary>
        private MessageManager? _messageManager;

        /// <summary>Multiplayer mod message handler for allowing clients to pause time.</summary>
        internal static MessageManager? MessageManager => ModEntry._instance?._messageManager;

        /// <summary>The mod monitor.</summary>
        internal static IMonitor? IMonitor => ModEntry._instance?.Monitor;

        /// <summary>The last notification sent via <see cref="ModEntry.UpdateSettingsForLocation"/>.</summary>
        private string? lastNotif { get; set; }= null;

        /// <summary>Internal host config option for config entry <see cref="ModFreezeTimeConfig.HostOnly"/>.</summary>
        private bool _hostHostOnly = true;

        /// <summary>Internal host config option for config entry <see cref="ModFreezeTimeConfig.ClientVote"/>.</summary>
        private bool _hostVoteEnabled = false;

        /// <summary>Internal host config option for config entry <see cref="ModFreezeTimeConfig.VoteThreshold"/>.</summary>
        private double _hostVoteThreshold = 1.0;

        /// <summary>External property for host config option for config entry <see cref="ModFreezeTimeConfig.HostOnly"/>.</summary>
        internal static bool HostHostOnly
        {
            get => ModEntry._instance._hostHostOnly;
            private set => ModEntry._instance._hostHostOnly = value;
        }

        /// <summary>External property for host config option for config entry <see cref="ModFreezeTimeConfig.ClientVote"/>.</summary>
        internal static bool HostVoteEnabled
        {
            get => ModEntry._instance._hostVoteEnabled;
            private set => ModEntry._instance._hostVoteEnabled = value;
        }

        /// <summary>External property for host config option for config entry <see cref="ModFreezeTimeConfig.VoteThreshold"/>.</summary>
        internal static double HostVoteThreshold
        {
            get => ModEntry._instance._hostVoteThreshold;
            private set => ModEntry._instance._hostVoteThreshold = value;
        }
#endregion

        /*********
         ** Public methods
         *********/
#region Public methods
        /// <inheritdoc />
        public ModEntry() {
            ModEntry._instance = this;
        }
#endregion

        /*********
         ** Internal methods
         *********/
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
			if (ModEntry._instance is null)
                return;
		    ModEntry._instance.HandleInputImpl(freezeTimeMethod, tickIntervalState, location, false);
		}

        /// <summary>Sets the local instanced host variables.</summary>
        /// <param name="hostOnly">The value of host's <see cref="ModFreezeTimeConfig.HostOnly"/> config option, <see langword="null"/> if unchanged.</param>
        /// <param name="voteEnabled">The value of host's <see cref="ModFreezeTimeConfig.ClientVote"/> config option, <see langword="null"/> if unchanged.</param>
        /// <param name="voteThreshold">The value of host's <see cref="ModFreezeTimeConfig.VoteThreshold"/> config option, <see langword="null"/> if unchanged.</param>
        private void SetHostConfigImpl(bool? hostOnly = null, bool? voteEnabled = null, double? voteThreshold = null)
        {
            if (hostOnly is { } _hostOnly && ModEntry.HostHostOnly != _hostOnly)
            {
                this.Notifier.QuickNotify(I18n.Message_HostChanged_HostOnly(state: GetLocalizedOnOffState(_hostOnly)), false);
                ModEntry.HostHostOnly = _hostOnly;
            }

            if (voteEnabled is { } _voteEnabled && ModEntry.HostVoteEnabled != _voteEnabled)
            {
                this.Notifier.QuickNotify(I18n.Message_HostChanged_VoteEnabled(state: GetLocalizedOnOffState(_voteEnabled)), false);
                ModEntry.HostVoteEnabled = _voteEnabled;
            }

            if (voteThreshold is { } _voteThreshold && ModEntry.HostVoteThreshold != _voteThreshold)
            {
                this.Notifier.QuickNotify(I18n.Message_HostChanged_VoteThreshold(value: ((int)(_voteThreshold * 100)).ToString()), false);
                ModEntry.HostVoteThreshold = _voteThreshold;
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

        /*********
        ** Private methods
        *********/
#region Private methods
        /****
        ** Event handlers
        ****/
#region Event handlers
        /**
        ** New
        **/
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
                voteThreshold: this.Config.FreezeTime.VoteThreshold
            );
        }

        /// <inheritdoc cref="IMultiplayerEvents.ModMessageReceived"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != base.ModManifest.UniqueID || this._messageManager is null)
                return;
            this._messageManager.HandleIncomingMessage(e);
        }
#endregion
        /**
        ** Patched
        **/
#region Patched
        /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
          GenericModConfigMenuIntegration.Register(this.ModManifest, this.Helper.ModRegistry, this.Monitor,
              getConfig: () => this.Config,
              reset: () => this.Config = new(),
              save: () =>
              {
                  this.Helper.WriteConfig(this.Config);
                  if (Context.IsWorldReady && this.ShouldEnable())
                      this.UpdateSettingsForLocationForAllPlayers();
              }
          );
        }

        /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
          if (!this._hostVoteEnabled && !this.Config.FreezeTime.HostOnly && !Context.IsMainPlayer)
              this.Monitor.Log("Disabled mod; only works for the main player in multiplayer.", LogLevel.Warn);
        }

        /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
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
        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            var freezeTimeMethod = this.Config.Keys.FreezeTime.JustPressed() ? FreezeTimeMethod.Manual : FreezeTimeMethod.None;
            bool? increase = null;

            if (this.Config.Keys.IncreaseTickInterval.JustPressed()) {
                increase = true;
            }

            if (this.Config.Keys.DecreaseTickInterval.JustPressed()) {
                increase = false;
            }

            bool reloadConfig = this.Config.Keys.ReloadConfig.JustPressed();
            this.HandleInputImpl(freezeTimeMethod, increase, null, reloadConfig);
        }

        /// <inheritdoc cref="IPlayerEvents.Warped"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnWarped(object sender, WarpedEventArgs e)
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
            Timer.IncrementTimers();

            if (!this.ShouldEnable())
                return;

            this.TimeHelper.Update();
        }
#endregion
#endregion

        /****
        ** Methods
        ****/
#region Methods
        private void InitializeNewStuff(IModHelper helper) {
            // Load managers
            this._messageManager = new MessageManager(this.Monitor, helper, this.Notifier, this.ModManifest.UniqueID);
            helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
            helper.Events.Multiplayer.PeerConnected += this.OnPeerConnected;
            this._timer = new Timer(this.Config.FreezeTime.ClientVoteTimeout);
            this._timer.OnFinished += this.OnTimerFinished;
        }

        private string ParseLogError(Exception exception, string? message)
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

        private void LogError(Exception exception, string? message = null, LogLevel logLevel = LogLevel.Error)
        {
            this.Monitor.Log(this.ParseLogError(exception, message), logLevel);
        }

        private void LogErrorOnce(Exception exception, string? message = null, LogLevel logLevel = LogLevel.Error)
        {
            this.Monitor.LogOnce(this.ParseLogError(exception, message), logLevel);
        }

        private Farmer? GetFarmer(ModMessageReceivedEventArgs e) {
            ITimeMessage genericMessage;

            try {
                genericMessage = e.ReadAs<ITimeMessage>();
            } catch (Exception exception) {
                this.LogErrorOnce(exception, "Failed to parse message as an ITimeMessage interface.");
                return null;
            }

            try {
                return Game1.getFarmer(genericMessage.FarmerID);
            } catch (Exception exception) {
                this.LogErrorOnce(exception, $"Failed to get farmer with the id {genericMessage.FarmerID}.");
            }

            return null;
        }

        /// <summary>Get whether time features should be enabled.</summary>
        /// <param name="forInput">Whether to check for input handling.</param>
        private bool ShouldEnable(bool forInput = false)
        {
            // is loaded and host player (farmhands can't change time)
            if (!Context.IsWorldReady || (this.Config.FreezeTime.HostOnly && !Context.IsMainPlayer))
                return false;

            // check restrictions for input
            if (!forInput)
                return true;

            // don't handle input when player isn't free (except in events)
            if (!Context.IsPlayerFree && !Game1.eventUp)
                return false;

            // ignore input if a textbox is active
            if (Game1.keyboardDispatcher.Subscriber is not null)
                return false;

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
        private void UpdateSettingsForLocation(GameLocation location)
        {
            if (location is null || this.Config is null)
                return;

            // update time settings
            this.UpdateTimeFreeze(location);
            this.TickInterval = this.Config.GetMillisecondsPerMinute(location) * 10;

            // notify player
            if (!this.Config.LocationNotify)
                return;

            string notif = this.AutoFreeze switch {
                AutoFreezeReason.FrozenAtTime when this.IsTimeFrozen => I18n.Message_OnLocationChange_TimeStoppedGlobally(),
                AutoFreezeReason.FrozenForLocation when this.IsTimeFrozen => I18n.Message_OnLocationChange_TimeStoppedHere(),
                AutoFreezeReason.FrozenDuringEvent when this.IsTimeFrozen => I18n.Message_OnLocationChange_TimeStoppedDuringEvent(
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
            return progress * this.TimeHelper.CurrentDefaultTickInterval / newTickInterval;
        }

        /// <summary>Get the freeze type which applies for the current context, ignoring overrides by the player.</summary>
        /// <param name="location">The game location.</param>
        private AutoFreezeReason GetAutoFreezeType(GameLocation? location = null)
        {
            if (location is not null && this.Config.ShouldFreeze(location))
                return AutoFreezeReason.FrozenForLocation;

            if (this.Config.ShouldFreeze(Game1.timeOfDay))
                return AutoFreezeReason.FrozenAtTime;

            if (this.Config.FreezeTime.DuringEvents && Game1.eventUp)
            {
                this._messageManager?.SendTimeManipulateMessage(farmer: Game1.player, freezeTimeMethod: FreezeTimeMethod.Event);
                return AutoFreezeReason.FrozenDuringEvent;
            }

            return AutoFreezeReason.None;
        }

        /// <summary>Tests if the <paramref name="farmerID"/> is the main farmer.</summary>
        /// <param name="farmerID">The <see cref="Farmer.UniqueMultiplayerID"/> of a farmer.</param>
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
        /// <param name="tickIntervalState">The state to change the tick interval to.</param>
        /// <param name="location">The location to pass from a client to the host.</param>
        /// <param name="reloadConfig">Whether to reload the config, applicable to the host only.</param>
        private void HandleInputImpl(FreezeTimeMethod freezeTimeMethod = FreezeTimeMethod.None, bool? increase = null, GameLocation? location = null, bool reloadConfig = false)
        {
            if (!this.ShouldEnable())
                return;

            if (!Context.IsMainPlayer)
            {
                if (increase is not null)
                    this._messageManager?.SendTimeManipulateMessage(
                        farmer: Game1.player,
                        increase: increase);
                else if (freezeTimeMethod is FreezeTimeMethod.Manual)
                    this._messageManager?.SendTimeManipulateForLocationMessage(
                        farmer: Game1.player,
                        freezeTimeMethod: freezeTimeMethod,
                        location: location);

                return;
            }

            if (!this.ShouldEnable(forInput: true))
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
}
