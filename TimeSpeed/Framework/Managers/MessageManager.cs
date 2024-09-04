using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using TimeSpeed.Framework.Models.Enum;
using TimeSpeed.Framework.Models.Messages;

#nullable enable

namespace TimeSpeed.Framework.Managers
{
    internal partial class MessageManager
    {
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        private readonly Notifier _notifier;
        private readonly string _modID;

        public MessageManager(IMonitor monitor, IModHelper helper, Notifier notifier, string modID)
        {
            this._monitor = monitor;
            this._helper = helper;
            this._notifier = notifier;
            this._modID = modID;
        }

        public void HandleIncomingMessage(ModMessageReceivedEventArgs e)
        {
            if (!Enum.TryParse(e.Type, out MessageType type))
            {
                this._monitor.LogOnce($"Failed to handle incoming message with type {e.Type}", LogLevel.Trace);
                return;
            }

            switch (type)
            {
                case MessageType.Manipulate:
                    this.ProcessTimeManipulateMessage(e.ReadAs<TimeManipulateMessage>());
                    break;
                case MessageType.ManipulateForLocation:
                    this.ProcessTimeManipulateForLocationMessage(e.ReadAs<TimeManipulateForLocationMessage>());
                    break;
                case MessageType.StateReply:
                    this.ProcessTimeStateReplyMessage(e.ReadAs<TimeStateReplyMessage>());
                    break;
                case MessageType.Forbidden:
                    this.ProcessTimeForbiddenMessage(e.ReadAs<TimeForbiddenMessage>());
                    break;
                case MessageType.ConfigState:
                    this.ProcessTimeConfigStateMessage(e.ReadAs<TimeConfigStateMessage>());
                    break;
                case MessageType.VotePause:
                    this.ProcessTimeVotePauseMessage(e.ReadAs<TimeVotePauseMessage>());
                    break;
                case MessageType.Info:
                    this.ProcessTimeInfoMessage(e.ReadAs<TimeInfoMessage>());
                    break;
            }
        }
    }
}
