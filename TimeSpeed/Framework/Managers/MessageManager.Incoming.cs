using StardewModdingAPI;
using StardewValley;
using TimeSpeed.Framework.Models.Enum;
using TimeSpeed.Framework.Models.Messages;

#nullable enable

namespace TimeSpeed.Framework.Managers
{
    internal partial class MessageManager
    {
        private void ProcessTimeForbiddenMessage(TimeForbiddenMessage message)
        {
            Farmer farmer = Game1.getFarmer(message.FarmerID);
            if (farmer is null || Game1.getFarmer(message.FarmerID).IsMainPlayer)
                return;
            this._monitor.LogOnce($"Got Time Forbidden Message {message.Reason} from farmer {farmer.Name}", LogLevel.Alert);
            string text = message.Reason switch
            {
                ForbiddenReason.HostDisabled => I18n.Message_Forbidden_HostDisabled(),
                ForbiddenReason.HostError    => I18n.Message_Forbidden_HostError(),
                _ => I18n.Message_Forbidden_Unknown()
            };
            this._notifier.ShortNotify(text);
        }

        private void ProcessTimeManipulateMessage(TimeManipulateMessage message)
        {
            Farmer farmer = Game1.getFarmer(message.FarmerID);
            if (farmer is null || Game1.getFarmer(message.FarmerID).IsMainPlayer)
                return;
            this._monitor.LogOnce($"Got Time Manipulate Message {{ FreezeTimeMethod: {message.FreezeTimeMethod} | Increase: {message.Increase} }} from farmer {farmer.Name}", LogLevel.Alert);
            ModEntry.HandleInput(message.FreezeTimeMethod, message.Increase);
        }

        private void ProcessTimeManipulateForLocationMessage(TimeManipulateForLocationMessage message)
        {
            Farmer farmer = Game1.getFarmer(message.FarmerID);
            if (farmer is null || !Game1.getFarmer(message.FarmerID).IsMainPlayer)
                return;
            this._monitor.LogOnce($"Got Time Manipulate For Location Message {{ FreezeTimeMethod: {message.FreezeTimeMethod} | Increase: {message.Increase} | GameLocation : {ModEntry.GetLocationFromID(message.Location)?.Name ?? "Unknown"} }} from farmer {farmer.Name}", LogLevel.Alert);
            ModEntry.HandleInput(message.FreezeTimeMethod, message.Increase, ModEntry.GetLocationFromID(message.Location));
        }

        private void ProcessTimeStateReplyMessage(TimeStateReplyMessage message)
        {
            Farmer farmer = Game1.getFarmer(message.FarmerID);
            if (farmer is null)
                return;
            this._monitor.LogOnce($"Got Time State Reply Message {{ \"Message: {message.Message}\" | Timeout: {message.Timeout} }} from farmer {farmer.Name}", LogLevel.Alert);
            Game1.addHUDMessage(new HUDMessage(message.Message, 2) { timeLeft = message.Timeout });
        }

        private void ProcessTimeConfigStateMessage(TimeConfigStateMessage message)
        {
            Farmer farmer = Game1.getFarmer(message.FarmerID);
            if (farmer is null)
                return;
            this._monitor.LogOnce($"Got Time Config State Message {{ HostOnly: {message.HostOnly} | VoteEnabled: {message.VoteEnabled} | VoteThreshold: {message.VoteThreshold} }} from farmer {farmer.Name}", LogLevel.Alert);
            ModEntry.SetHostConfig(message.HostOnly, message.VoteEnabled, message.VoteThreshold);
        }

        private void ProcessTimeInfoMessage(TimeInfoMessage message)
        {
            Farmer farmer = Game1.getFarmer(message.FarmerID);
            if (farmer is null)
                return;
            this._monitor.LogOnce($"Got Time Info Message {{ HostOnly: \"{message.Message}\" }} from farmer {farmer.Name}", LogLevel.Alert);
            if (message.Message is null)
                return;
            this._notifier.ShortNotify(message.Message, false);
        }

        private void ProcessTimeVotePauseMessage(TimeVotePauseMessage message)
        {
            Farmer farmer = Game1.getFarmer(message.FarmerID);
            if (farmer is null)
                return;
            this._monitor.LogOnce($"Sent Time Vote Pause Message {{ VoteCast: {message.VoteCast} | Finish: {message.Finish} }} from farmer {farmer.Name}", LogLevel.Alert);
            ModEntry.PushVoteCast(message.FarmerID, message.VoteCast, message.Finish);
        }
    }
}
