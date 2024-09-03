using System;
using System.Text;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using TimeSpeed.Framework.Models.Enum;
using TimeSpeed.Framework.Models.Messages;

namespace TimeSpeed.Framework.Managers
{
    internal partial class MessageManager
    {
        public void HandleIncomingMessage(ModMessageReceivedEventArgs e)
        {
            if (!Enum.TryParse(e.Type, out MessageType type))
            {
                this._monitor.LogOnce("Failed to handle incoming message with type " + e.Type, LogLevel.Trace);
                return;
            }

            switch (type)
            {
                case MessageType.Manipulate:
                    MessageManager.ProcessTimeManipulateMessage(e.ReadAs<TimeManipulateMessage>());
                    break;
                case MessageType.ManipulateForLocation:
                    MessageManager.ProcessTimeManipulateForLocationMessage(e.ReadAs<TimeManipulateForLocationMessage>());
                    break;
                case MessageType.StateReply:
                    MessageManager.ProcessTimeStateReplyMessage(e.ReadAs<TimeStateReplyMessage>());
                    break;
                case MessageType.Forbidden:
                    MessageManager.ProcessTimeForbiddenMessage(e.ReadAs<TimeForbiddenMessage>());
                    break;
            }
        }

        private static void ProcessTimeForbiddenMessage(TimeForbiddenMessage message)
        {
            Farmer farmer = Game1.getFarmer(message.FarmerID);
            if (farmer is null || ModEntry.IsMainFarmer(message.FarmerID))
                return;
            string text = message.Reason switch
            {
                ForbiddenReason.HostDisabled => I18n.Message_Forbidden_HostDisabled(),
                ForbiddenReason.HostError    => I18n.Message_Forbidden_HostError(),
                _ => I18n.Message_Forbidden_Unknown()
            };
            Notifier.ShortNotify(text);
        }

        private static void ProcessTimeManipulateMessage(TimeManipulateMessage message)
        {
            Farmer farmer = Game1.getFarmer(message.FarmerID);
            if (farmer is null || ModEntry.IsMainFarmer(message.FarmerID))
                return;
            ModEntry.HandleInputBridge(message.FreezeTimeMethod, TickIntervalState.None);
        }

        private static void ProcessTimeManipulateForLocationMessage(TimeManipulateForLocationMessage message)
        {
            Farmer farmer = Game1.getFarmer(message.FarmerID);
            if (farmer is null)
                return;
            ModEntry.HandleInputBridge(message.FreezeTimeMethod, TickIntervalState.None, ModEntry.GetLocationFromID(message.Location));
        }

        private static void ProcessTimeStateReplyMessage(TimeStateReplyMessage message)
        {
            Farmer farmer = Game1.getFarmer(message.FarmerID);
            if (farmer is null)
                return;
            Game1.addHUDMessage(new HUDMessage(Encoding.Default.GetString(message.Message), 2) { timeLeft = message.Timeout });
        }

        private static void ProcessTimeConfigStateMessage(TimeConfigStateMessage message)
        {
            Farmer farmer = Game1.getFarmer(message.FarmerID);
            if (farmer is null)
                return;
            ModEntry.SetHostConfig(message.HostOnly, message.VoteEnabled, message.VoteThreshold);
        }
    }
}
