using System.Text;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using StardewValley;
using TimeSpeed.Framework.Models.Enum;
using TimeSpeed.Framework.Models.Messages;

namespace TimeSpeed.Framework.Managers
{
    internal partial class MessageManager
    {
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        private readonly string _modID;
        private const string Separator = "$/$";
        private static readonly Regex separatorRegex = new(Regex.Escape(Separator));

        public MessageManager(IMonitor monitor, IModHelper helper, string modID)
        {
            this._monitor = monitor;
            this._helper = helper;
            this._modID = modID;
        }

        public void SendTimeManipulateMessage(Farmer farmer, FreezeTimeMethod freezeTimeMethod = FreezeTimeMethod.None, TickIntervalState tickIntervalState = TickIntervalState.None)
        {
            if (farmer is null)
                return;

            var timeManipulateMessage = new TimeManipulateMessage {
                FarmerID = farmer.UniqueMultiplayerID,
                FreezeTimeMethod = freezeTimeMethod,
                TickIntervalState = tickIntervalState,
            };

            this._helper.Multiplayer.SendMessage(timeManipulateMessage, nameof(MessageType.Manipulate), modIDs: [ this._modID ]);
        }

        public void SendTimeManipulateForLocationMessage(Farmer farmer, GameLocation? location, FreezeTimeMethod freezeTimeMethod = FreezeTimeMethod.None, TickIntervalState tickIntervalState = TickIntervalState.None)
        {
            if (farmer is null || location is null)
                return;

            var timeManipulateMessageForLocation = new TimeManipulateForLocationMessage
            {
                FarmerID = farmer.UniqueMultiplayerID,
                FreezeTimeMethod = freezeTimeMethod,
                TickIntervalState = tickIntervalState,
                Location = ModEntry.GetIDFromLocation(location),
            };

            this._helper.Multiplayer.SendMessage(timeManipulateMessageForLocation, nameof(MessageType.ManipulateForLocation), modIDs: [ this._modID ]);
        }

        public void SendTimeStateReply(Farmer farmer, int timeout, string message)
        {
            if (farmer is null)
                return;

            var _message = new TimeStateReplyMessage
            {
                FarmerID = farmer.UniqueMultiplayerID,
                Timeout = timeout,
                Message = Encoding.Default.GetBytes(message),
            };

            this._helper.Multiplayer.SendMessage(_message, nameof(MessageType.StateReply), modIDs: [ this._modID ]);
        }

        public void SendTimeForbiddenMessage(Farmer? farmer, ForbiddenReason reason) {
            if (farmer is null)
                return;

            var message = new TimeForbiddenMessage
            {
                FarmerID = farmer.UniqueMultiplayerID,
                Reason = reason,
            };

            this._helper.Multiplayer.SendMessage(message, nameof(MessageType.Forbidden), modIDs: [ this._modID]);
        }

        public void SendTimeInfoMessage(Farmer? farmer, string message)
        {
            if (farmer is null)
                return;

            var _message = new TimeInfoMessage
            {
                FarmerID = farmer.UniqueMultiplayerID,
                Message = Encoding.Default.GetBytes(message),
            };

            this._helper.Multiplayer.SendMessage(_message, nameof(MessageType.Info), modIDs: [ this._modID]);
        }

        public void SendTimeVotePauseMessage(Farmer? farmer, VoteCast voteCast = VoteCast.None, bool finish = false)
        {
            if (farmer is null)
                return;

            var message = new TimeVotePauseMessage
            {
                FarmerID = farmer.UniqueMultiplayerID,
                VoteCast = voteCast,
                Finish = finish,
            };

            this._helper.Multiplayer.SendMessage(message, nameof(MessageType.VotePause), modIDs: [ this._modID]);
        }

        public void SendTimeConfigStateMessage(Farmer? farmer, bool hostOnly = true, bool voteEnabled = false, double voteThreshold = 1.0)
        {
            if (farmer is null)
                return;

            var message = new TimeConfigStateMessage
            {
                FarmerID = farmer.UniqueMultiplayerID,
                HostOnly= hostOnly,
                VoteEnabled = voteEnabled,
                VoteThreshold = voteThreshold,
            };

            this._helper.Multiplayer.SendMessage(message, nameof(MessageType.VotePause), modIDs: [ this._modID]);
        }
    }
}
