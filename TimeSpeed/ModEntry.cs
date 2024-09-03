using System;
using System.Linq;
using System.Text;
using cantorsdust.Common;
using cantorsdust.Common.Extensions;

using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using TimeSpeed.Framework;
using TimeSpeed.Framework.Managers;
using TimeSpeed.Framework.Models.Enum;
using TimeSpeed.Framework.Models.Messages;
using TimeSpeed.Framework.Models.Messages.Interfaces;

namespace TimeSpeed
{
    /// <summary>The entry class called by SMAPI.</summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    internal partial class ModEntry : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>Displays messages to the user.</summary>
        private static ModEntry _instance = null!;

        /// <summary>Provides helper methods for tracking time flow.</summary>
        private readonly TimeHelper TimeHelper = new();

        /// <summary>The mod configuration.</summary>
        private ModConfig Config = null!;

        /// <summary>Whether the player has manually frozen (<c>true</c>) or resumed (<c>false</c>) time.</summary>
        private bool? ManualFreeze;

        /// <summary>(Local) Multiplayer mod message handler for allowing clients to pause time.</summary>
        private MessageManager? _messageManager;
        /// <summary>Multiplayer mod message handler for allowing clients to pause time.</summary>
        internal static MessageManager? MessageManager => ModEntry._instance?._messageManager;

        /// <summary>The reason time would be frozen automatically if applicable, regardless of <see cref="ManualFreeze"/>.</summary>
        private AutoFreezeReason AutoFreeze = AutoFreezeReason.None;

        /// <summary>Whether time should be frozen.</summary>
        private bool IsTimeFrozen =>
            this.ManualFreeze == true
            || (this.AutoFreeze != AutoFreezeReason.None && this.ManualFreeze != false);

        /// <summary>Whether the flow of time should be adjusted.</summary>
        private bool AdjustTime;

        /// <summary>Backing field for <see cref="TickInterval"/>.</summary>
        private int _tickInterval;

        /// <summary>The number of milliseconds per 10-game-minutes to apply.</summary>
        private int TickInterval
        {
            get => this._tickInterval;
            set => this._tickInterval = Math.Max(value, 0);
        }

        private bool _hostClientDisabled = true;
        private bool _hostVoteEnabled = false;
        private double _hostVoteThreshold = 1.0;
        internal static bool HostClientDisabled {
            get => ModEntry._instance._hostClientDisabled;
            private set => ModEntry._instance._hostClientDisabled = value;
        }
        internal static bool HostVoteEnabled {
            get => ModEntry._instance._hostVoteEnabled;
            private set => ModEntry._instance._hostVoteEnabled = value;
        }
        internal static double HostVoteThreshold {
            get => ModEntry._instance._hostVoteThreshold;
            private set => ModEntry._instance._hostVoteThreshold = value;
        }

        internal static void SetHostConfig(bool clientDisabled, bool voteEnabled, double voteThreshold) {
            if (ModEntry.HostClientDisabled != clientDisabled) {
                Notifier.QuickNotify(I18n.Message_HostChanged_HostOnly(state: clientDisabled ? "on" : "off"));
            }
            ModEntry.HostClientDisabled = clientDisabled;
            if (ModEntry.HostVoteEnabled != voteEnabled) {
                Notifier.QuickNotify(I18n.Message_HostChanged_VoteEnabled(state: voteEnabled ? "on" : "off"));
            }
            ModEntry.HostVoteEnabled = voteEnabled;
            if (ModEntry.HostVoteThreshold != voteThreshold) {
                Notifier.QuickNotify(I18n.Message_HostChanged_VoteThreshold(value: ((int)(voteThreshold * 100)).ToString()));
            }
            ModEntry.HostVoteThreshold = voteThreshold;
        }


        /*********
         ** Public methods
         *********/
        /// <inheritdoc />
        public ModEntry() {
            ModEntry._instance = this;
        }

        /// <inheritdoc />
        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);
            CommonHelper.RemoveObsoleteFiles(this, "TimeSpeed.pdb");

            // read config
            this.Config = helper.ReadConfig<ModConfig>();

            // Load managers
            this._messageManager = new MessageManager(this.Monitor, helper, this.ModManifest.UniqueID);

            // add time events
            this.TimeHelper.WhenTickProgressChanged(this.OnTickProgressed);
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
            helper.Events.Player.Warped += this.OnWarped;
            helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
            helper.Events.Multiplayer.PeerConnected += this.OnPeerConnected;

            // add time freeze/unfreeze notification
            {
                bool wasPaused = false;
                helper.Events.Display.RenderingHud += (_, _) =>
                {
                    wasPaused = Game1.paused;
                    if (this.IsTimeFrozen)
                        Game1.paused = true;
                };

                helper.Events.Display.RenderedHud += (_, _) =>
                {
                    Game1.paused = wasPaused;
                };
            }
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
                voteThreshold: this.Config.FreezeTime.ClientVoteThreshold
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
            /*
            if (!Enum.TryParse(e.Type, out MessageType type))
            {
                this.Monitor.LogOnce($"Failed to handle incoming message with type {e.Type}");
                return;
            }

            Farmer? farmer = this.GetFarmer(e);
            if (farmer is null || e.FromModID != this.ModManifest.UniqueID)
                return;

            switch (type)
            {
                case MessageType.Manipulate when Context.IsMainPlayer:
                    var message = e.ReadAs<TimeManipulateMessage>();
                    this.HandleInput(freezeTimeMethod: message.FreezeTimeMethod);
                    break;
                case MessageType.ManipulateForLocation when Context.IsMainPlayer:
                    var locMessage = e.ReadAs<TimeManipulateMessageForLocation>();
                    this.HandleInput(freezeTimeMethod: locMessage.FreezeTimeMethod,
                                     location: ModEntry.GetLocationFromID(locMessage.Location));
                    break;
                case MessageType.StateReply:
                    var reply = e.ReadAs<TimeStateReplyMessage>();
                    Game1.addHUDMessage(new HUDMessage(Encoding.Default.GetString(reply.Message), HUDMessage.newQuest_type) { timeLeft = reply.Timeout });
                    break;
                case MessageType.Forbidden when !Context.IsMainPlayer:
                    var forbidden = e.ReadAs<TimeManipulateForbiddenMessage>();
                    string _message = forbidden.Reason switch
                    {
                        ForbiddenReason.HostDisabled => I18n.Message_Forbidden_HostDisabled(),
                        ForbiddenReason.HostError    => I18n.Message_Forbidden_HostError(),
                        _ => I18n.Message_Forbidden_Unknown()
                    };
                    Notifier.ShortNotify(_message);
                    break;
                case MessageType.VotePause when !Context.IsMainPlayer:
                    if ()
                    var votePause = e.ReadAs<TimeVotePauseMessage>();
                    if (!this.VoteHappening)
                    {
                        Notifier.ShortNotify(I18n.Message_Unknown(farmer: farmer.Name));
                        this.StartVote(farmer);
                    }
                    if (Context.IsMainPlayer)
                    {

                    }
                    else
                    {
                    }
                    break;
                case MessageType.Info:
                    var info = e.ReadAs<TimeInfoMessage>();
                    if (this.Config.DisplayVotePauseMessages)
                        Notifier.QuickNotify(Encoding.Default.GetString(info.Message));
                    break;
                case MessageType.Unknown:
                    Notifier.ShortNotify(I18n.Message_Unknown(farmer: farmer.Name));
                    break;
                default:
                    break;
            }
            */
        }


        /*********
        ** Private methods
        *********/
        /****
        ** Event handlers
        ****/
        /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
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
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            if (this.Config.FreezeTime.HostOnly && !Context.IsMainPlayer && this._hostClientDisabled)
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
            this.HandleInput(
                freezeTimeMethod:
                    this.Config.Keys.FreezeTime.JustPressed() ?
                        FreezeTimeMethod.Toggle : FreezeTimeMethod.None,
                tickIntervalState:
                    this.Config.Keys.IncreaseTickInterval.JustPressed() ?
                        TickIntervalState.Increase :
                            this.Config.Keys.DecreaseTickInterval.JustPressed() ?
                                TickIntervalState.Decrease : TickIntervalState.None,
                reloadConfig:
                    this.Config.Keys.ReloadConfig.JustPressed()
            );
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

        /// <inheritdoc cref="IGameLoopEvents.TimeChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
        {
            if (!this.ShouldEnable())
                return;

            this.UpdateFreezeForTime();
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

        /// <summary>Raised after the <see cref="Framework.TimeHelper.TickProgress"/> value changes.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnTickProgressed(object? sender, TickProgressChangedEventArgs e)
        {
            if (!this.ShouldEnable())
                return;

            if (this.IsTimeFrozen)
                this.TimeHelper.TickProgress = e.TimeChanged ? 0 : e.PreviousProgress;
            else
            {
                if (!this.AdjustTime)
                    return;
                if (this.TickInterval == 0)
                    this.TickInterval = 1000;

                if (e.TimeChanged)
                    this.TimeHelper.TickProgress = this.ScaleTickProgress(this.TimeHelper.TickProgress, this.TickInterval);
                else
                    this.TimeHelper.TickProgress = e.PreviousProgress + this.ScaleTickProgress(e.NewProgress - e.PreviousProgress, this.TickInterval);
            }
        }

        /****
        ** Methods
        ****/
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
            Notifier.ShortNotify(I18n.Message_ConfigReloaded());
        }

        /// <summary>Increment or decrement the tick interval, taking into account the held modifier key if applicable.</summary>
        /// <param name="increase">Whether to increment the tick interval; else decrement.</param>
        private void ChangeTickInterval(bool increase)
        {
            // get offset to apply
            int change = 1000;
            {
                KeyboardState state = Keyboard.GetState();
                if (state.IsKeyDown(Keys.LeftControl))
                    change *= 100;
                else if (state.IsKeyDown(Keys.LeftShift))
                    change *= 10;
                else if (state.IsKeyDown(Keys.LeftAlt))
                    change /= 10;
            }

            // update tick interval
            if (!increase)
            {
                int minAllowed = Math.Min(this.TickInterval, change);
                this.TickInterval = Math.Max(minAllowed, this.TickInterval - change);
            }
            else
                this.TickInterval += change;

            // log change
            Notifier.QuickNotify(
                I18n.Message_SpeedChanged(seconds: this.TickInterval / 1000)
            );
            this.Monitor.Log($"Tick length set to {this.TickInterval / 1000d: 0.##} seconds.", LogLevel.Info);
        }

        /// <summary>Toggle whether time is frozen.</summary>
        private void ToggleFreeze()
        {
            if (!this.IsTimeFrozen)
            {
                this.UpdateTimeFreeze(manualOverride: true);
                Notifier.QuickNotify(I18n.Message_TimeStopped());
                this.Monitor.Log("Time is frozen globally.", LogLevel.Info);
            }
            else
            {
                this.UpdateTimeFreeze(manualOverride: false);
                Notifier.QuickNotify(I18n.Message_TimeResumed());
                this.Monitor.Log($"Time is resumed at \"{Game1.currentLocation.Name}\".", LogLevel.Info);
            }
        }

        /// <summary>Update the time freeze settings for the given time of day.</summary>
        private void UpdateFreezeForTime() {
            bool wasFrozen = this.IsTimeFrozen;
            this.UpdateTimeFreeze();

            if (wasFrozen || !this.IsTimeFrozen) {
                return;
            }

            Notifier.ShortNotify(I18n.Message_OnTimeChange_TimeStopped());
            this.Monitor.Log($"Time automatically set to frozen at {Game1.timeOfDay}.", LogLevel.Info);
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

        private string? lastNotif;

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
                _ => I18n.Message_OnLocationChange_TimeSpeedHere(seconds: this.TickInterval / 1000),
            };

            if (this.lastNotif?.Equals(notif, StringComparison.OrdinalIgnoreCase) == true)
                return;

            Notifier.ShortNotify(notif);
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
                this.Monitor.Log($"Auto freeze changed from {wasAutoFreeze} to {this.AutoFreeze}.");
            if (wasManualFreeze != this.ManualFreeze)
                this.Monitor.Log($"Manual freeze changed from {wasManualFreeze?.ToString() ?? "null"} to {this.ManualFreeze?.ToString() ?? "null"}.");
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

            return AutoFreezeReason.None;
        }

        /// <summary>Tests if the <paramref name="farmerID"/> is the main farmer.</summary>
        /// <param name="farmerID">The <see cref="Farmer.UniqueMultiplayerID"/> of a farmer.</param>
        internal static bool IsMainFarmer(long farmerID)
        {
            return !Context.HasRemotePlayers || Game1.getFarmer(farmerID).IsMainPlayer;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>Gets a <see cref="GameLocation"/> from the location's <see cref="GameLocation.NameOrUniqueName"/>.</summary>
        /// <param name="location">The <see cref="GameLocation.NameOrUniqueName"/> of a <see cref="GameLocation"/>.</param>
        internal static GameLocation? GetLocationFromID(string location)
        {
            return Game1.locations.FirstOrDefault(x => x.NameOrUniqueName == location);
        }

        /// <summary>Gets a <see cref="GameLocation"/> from the location's <see cref="GameLocation.NameOrUniqueName"/> as a byte array.</summary>
        /// <param name="location">The <see cref="GameLocation.NameOrUniqueName"/> of a <see cref="GameLocation"/> as a byte array.</param>
        internal static GameLocation? GetLocationFromID(byte[] location)
        {
            if (location.Length == 0)
                return null;

            return ModEntry.GetLocationFromID(Encoding.Default.GetString(location));
        }

        /// <summary>Gets a <see cref="GameLocation"/>'s <see cref="GameLocation.NameOrUniqueName"/> as a byte array.</summary>
        /// <param name="location">The instance of a game location.</param>
        internal static byte[] GetIDFromLocation(GameLocation? location)
        {
            return location is null ? [] : Encoding.Default.GetBytes(location.NameOrUniqueName);
        }

        /// <summary>Wrapper for handling inputs from a client or a host.</summary>
        /// <param name="freezeTimeMethod">The method of freezing time.</param>
        /// <param name="tickIntervalState">The state to change the tick interval to.</param>
        /// <param name="location">The location to pass from a client to the host.</param>
        /// <param name="reloadConfig">Whether to reload the config, applicable to the host only.</param>
        private void HandleInput(FreezeTimeMethod freezeTimeMethod = FreezeTimeMethod.None, TickIntervalState tickIntervalState = TickIntervalState.None, GameLocation? location = null, bool reloadConfig = false)
        {
            if (!this.ShouldEnable())
                return;

            if (!Context.IsMainPlayer)
            {
                if (tickIntervalState != TickIntervalState.None)
                    this._messageManager?.SendTimeManipulateMessage(Game1.player, tickIntervalState: tickIntervalState);
                else if (freezeTimeMethod is FreezeTimeMethod.Toggle)
                    this._messageManager?.SendTimeManipulateForLocationMessage(Game1.player, freezeTimeMethod: freezeTimeMethod, location: location);

                return;
            }

            if (!this.ShouldEnable(forInput: true))
                return;

            if (freezeTimeMethod == FreezeTimeMethod.Toggle)
                this.ToggleFreeze();
            else if (tickIntervalState != TickIntervalState.None)
                this.ChangeTickInterval(increase: tickIntervalState == TickIntervalState.Increase);
            else if (reloadConfig)
                this.ReloadConfig();
        }
    }
}
