using StardewModdingAPI;
using StardewValley;
using TimeSpeed.Framework.Models.Enum;
using TimeSpeed.Framework.Models.Messages;

#nullable enable

namespace TimeSpeed.Framework.Managers
{
    internal partial class MessageManager
    {
        public void SendTimeManipulateMessage(Farmer farmer, FreezeTimeMethod freezeTimeMethod = FreezeTimeMethod.None, bool? increase = null)
        {
            if (farmer is null)
                return;

            var timeManipulateMessage = new TimeManipulateMessage {
                FarmerID = farmer.UniqueMultiplayerID,
                FreezeTimeMethod = freezeTimeMethod,
                Increase = increase,
            };

            this._monitor.LogOnce($"Sent Time Manipulate Message {{ FreezeTimeMethod: {freezeTimeMethod} | Increase: {increase} }} from farmer {farmer.Name}", LogLevel.Alert);
            this._helper.Multiplayer.SendMessage(timeManipulateMessage, nameof(MessageType.Manipulate), modIDs: [ this._modID ]);
        }

        public void SendTimeManipulateForLocationMessage(Farmer farmer, GameLocation? location, FreezeTimeMethod freezeTimeMethod = FreezeTimeMethod.None, bool? increase = null)
        {
            if (farmer is null || location is null)
                return;

            var timeManipulateMessageForLocation = new TimeManipulateForLocationMessage
            {
                FarmerID = farmer.UniqueMultiplayerID,
                FreezeTimeMethod = freezeTimeMethod,
                Increase = increase,
                Location = ModEntry.GetIDFromLocation(location),
            };

            this._monitor.LogOnce($"Sent Time Manipulate For Location Message {{ FreezeTimeMethod: {freezeTimeMethod} | Increase: {increase} | GameLocation : {location?.Name ?? "Unknown"} }} from farmer {farmer.Name}", LogLevel.Alert);
            this._helper.Multiplayer.SendMessage(timeManipulateMessageForLocation, nameof(MessageType.ManipulateForLocation), modIDs: [ this._modID ]);
        }

        public void SendTimeStateReply(Farmer farmer, float timeout, string message)
        {
            if (farmer is null)
                return;

            var _message = new TimeStateReplyMessage
            {
                FarmerID = farmer.UniqueMultiplayerID,
                Timeout = timeout,
                Message = message,
            };

            this._monitor.LogOnce($"Sent Time State Reply Message {{ Message: \"{message}\" | Timeout: {timeout} }} from farmer {farmer.Name}", LogLevel.Alert);
            this._helper.Multiplayer.SendMessage(_message, nameof(MessageType.StateReply), modIDs: [ this._modID ]);
        }

        public void SendTimeInfoMessage(Farmer? farmer, string? message)
        {
            if (farmer is null)
                return;

            var _message = new TimeInfoMessage
            {
                FarmerID = farmer.UniqueMultiplayerID,
                Message = message,
            };

            this._monitor.LogOnce($"Sent Time Info Message {{ Message: \"{message}\" }} from farmer {farmer.Name}", LogLevel.Alert);
            this._helper.Multiplayer.SendMessage(_message, nameof(MessageType.Info), modIDs: [ this._modID]);
        }

        public void SendTimeForbiddenMessage(Farmer? farmer, ForbiddenReason reason) {
            if (farmer is null)
                return;

            var message = new TimeForbiddenMessage
            {
                FarmerID = farmer.UniqueMultiplayerID,
                Reason = reason,
            };

            this._monitor.LogOnce($"Sent Time Forbidden Message {{ Reason: {reason} }} from farmer {farmer.Name}", LogLevel.Alert);
            this._helper.Multiplayer.SendMessage(message, nameof(MessageType.Forbidden), modIDs: [ this._modID]);
        }

        public void SendTimeVotePauseMessage(Farmer? farmer, bool? voteCast = null, bool? finish = null)
        {
            if (farmer is null)
                return;

            var message = new TimeVotePauseMessage
            {
                FarmerID = farmer.UniqueMultiplayerID,
                VoteCast = voteCast,
                Finish = finish,
            };

            this._monitor.LogOnce($"Sent Time Vote Pause Message {{ VoteCast: {voteCast} | Finish: {finish} }} from farmer {farmer.Name}", LogLevel.Alert);
            this._helper.Multiplayer.SendMessage(message, nameof(MessageType.VotePause), modIDs: [ this._modID]);
        }

        /// <summary>Method to update the client </summary>
        /// <param name="farmer"></param>
        /// <param name="hostOnly"></param>
        /// <param name="voteEnabled"></param>
        /// <param name="voteThreshold"></param>
        public void SendTimeConfigStateMessage(Farmer? farmer, bool? hostOnly = null, bool? voteEnabled = null, double? voteThreshold = null)
        {
            if (farmer is null || !farmer.IsMainPlayer)
                return;

            var message = new TimeConfigStateMessage
            {
                FarmerID = farmer.UniqueMultiplayerID,
                HostOnly= hostOnly,
                VoteEnabled = voteEnabled,
                VoteThreshold = voteThreshold,
            };

            this._monitor.LogOnce($"Sent Time Config State Message {{ HostOnly: {hostOnly} | VoteEnabled: {voteEnabled} | VoteThreshold: {voteThreshold} }} from farmer {farmer.Name}", LogLevel.Alert);
            this._helper.Multiplayer.SendMessage(message, nameof(MessageType.ConfigState), modIDs: [ this._modID]);
        }
    }
}
