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
        /*
        public string EncodeMessage(Farmer farmer, string message)
        {
            return new StringBuilder()
                .Append(farmer.UniqueMultiplayerID)
                .Append(MessageManager.seperator)
                .AppendSimple(message).ToString();
        }

        public string EncodeMessage(Farmer farmer, string message, int timeout)
        {
            return new StringBuilder()
                .Append(farmer.UniqueMultiplayerID)
                .Append(MessageManager.seperator)
                .AppendSimple(message)
                .Append(MessageManager.seperator)
                .AppendSimple(timeout).ToString();
        }

        public string EncodeMessage(Farmer farmer, FreezeTimeMethod freezeTimeMethod = FreezeTimeMethod.None, TickIntervalState tickIntervalState = TickIntervalState.None)
        {
            return new StringBuilder()
                .Append(farmer.UniqueMultiplayerID)
                .Append(MessageManager.seperator)
                .AppendSimple(freezeTimeMethod)
                .Append(MessageManager.seperator)
                .AppendSimple(tickIntervalState).ToString();
        }

        public string EncodeMessage(Farmer farmer, GameLocation location, FreezeTimeMethod freezeTimeMethod = FreezeTimeMethod.None, TickIntervalState tickIntervalState = TickIntervalState.None)
        {
            return new StringBuilder()
                .Append(farmer.UniqueMultiplayerID)
                .Append(MessageManager.seperator)
                .AppendSimple(freezeTimeMethod)
                .Append(MessageManager.seperator)
                .AppendSimple(tickIntervalState)
                .Append(MessageManager.seperator)
                .AppendSimple(ModEntry.GetIDFromLocation(location)).ToString();
        }

        public string EncodeMessage(Farmer farmer, ForbiddenReason reason)
        {
            return new StringBuilder()
                .Append(farmer.UniqueMultiplayerID)
                .Append(MessageManager.seperator)
                .AppendSimple(reason).ToString();
        }

        public string EncodeMessage(Farmer farmer, VoteCast voteCast = VoteCast.None, bool finish = false)
        {
            return new StringBuilder()
                .Append(farmer.UniqueMultiplayerID)
                .Append(MessageManager.seperator)
                .AppendSimple(reason).ToString();
        }
        */
    }
}