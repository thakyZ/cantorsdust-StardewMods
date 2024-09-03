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
        public bool DecodeMessage(string value, out long farmerID, out string message)
        {
            var split = value.Split(MessageManager.seperator);

            if (split.Length != 2)
                this._monitor.LogOnce($"Expected message length of 3, got {split.Length}", LogLevel.Error); return false;

            if (!long.TryParse(split[0], out farmerID))
                this._monitor.LogOnce("Failed to parse long in position 0 of network message.", LogLevel.Error); return false;

            if (!split[1].TrimQuotes(out message))
                this._monitor.LogOnce("Failed to parse string in position 1 of network message.", LogLevel.Error); return false;

            return true;
        }

        public static bool DecodeMessage(string value, out long farmerID, out string message, out int timeout)
        {
            var split = value.Split(MessageManager.seperator);

            if (split.Length != 3)
                this._monitor.LogOnce($"Expected message length of 3, got {split.Length}", LogLevel.Error); return false;

            if (!long.TryParse(split[0], out farmerID))
                this._monitor.LogOnce("Failed to parse long in position 0 of network message.", LogLevel.Error); return false;

            if (!split[1].TryTrimQuotes(out message))
                this._monitor.LogOnce("Failed to parse string in position 1 of network message.", LogLevel.Error); return false;

            if (!int.TryParse(split[2], out timeout))
                this._monitor.LogOnce("Failed to parse int in position 2 of network message.", LogLevel.Error); return false;

            return true;
        }

        public string EncodeMessage(string value, out long farmerID, out FreezeTimeMethod freezeTimeMethod, out TickIntervalState tickIntervalState)
        {
            var split = value.Split(MessageManager.seperator);

            if (split.Length != 3)
                this._monitor.LogOnce($"Expected message length of 3, got {split.Length}", LogLevel.Error); return false;

            if (!long.TryParse(split[0], out farmerID))
                this._monitor.LogOnce("Failed to parse long in position 0 of network message.", LogLevel.Error); return false;

            if (!Enum.TryParse(typeof(FreezeTimeMethod), split[1], out freezeTimeMethod))
                this._monitor.LogOnce("Failed to parse enum in position 1 of network message.", LogLevel.Error); return false;

            if (!Enum.TryParse(typeof(TickIntervalState), split[2], out tickIntervalState))
                this._monitor.LogOnce("Failed to parse enum in position 2 of network message.", LogLevel.Error); return false;

            return true;
        }

        public string EncodeMessage(string value, out long farmerID, out GameLocation location, out FreezeTimeMethod freezeTimeMethod, out TickIntervalState tickIntervalState)
        {
            var split = value.Split(MessageManager.seperator);

            if (split.Length != 4)
                this._monitor.LogOnce($"Expected message length of 3, got {split.Length}", LogLevel.Error); return false;

            if (!long.TryParse(split[0], out farmerID))
                this._monitor.LogOnce("Failed to parse long in position 0 of network message.", LogLevel.Error); return false;

            if (ModEntry.GetLocationFromID(split[1]) is null)
                this._monitor.LogOnce("Failed to parse GameLocation in position 1 of network message.", LogLevel.Error); return false;

            if (!Enum.TryParse(typeof(FreezeTimeMethod), split[2], out freezeTimeMethod))
                this._monitor.LogOnce("Failed to parse enum in position 2 of network message.", LogLevel.Error); return false;

            if (!Enum.TryParse(typeof(TickIntervalState), split[3], out tickIntervalState))
                this._monitor.LogOnce("Failed to parse enum in position 3 of network message.", LogLevel.Error); return false;

            return true;
        }

        public string EncodeMessage(string value, out long farmerID, out ForbiddenReason reason)
        {
            var split = value.Split(MessageManager.seperator);

            if (split.Length != 2)
                this._monitor.LogOnce($"Expected message length of 3, got {split.Length}", LogLevel.Error); return false;

            if (!long.TryParse(split[0], out farmerID))
                this._monitor.LogOnce("Failed to parse long in position 0 of network message.", LogLevel.Error); return false;

            if (!Enum.TryParse(typeof(ForbiddenReason), split[1], out reason))
                this._monitor.LogOnce("Failed to parse enum in position 1 of network message.", LogLevel.Error); return false;

            return true;
        }

        public string EncodeMessage(string value, out long farmerID, out VoteCast voteCast, out bool finish)
        {
            var split = value.Split(MessageManager.seperator);

            if (split.Length != 3)
                this._monitor.LogOnce($"Expected message length of 3, got {split.Length}", LogLevel.Error); return false;

            if (!long.TryParse(split[0], out farmerID))
                this._monitor.LogOnce("Failed to parse long in position 0 of network message.", LogLevel.Error); return false;

            if (!Enum.TryParse(typeof(VoteCast), split[1], out voteCast))
                this._monitor.LogOnce("Failed to parse enum in position 1 of network message.", LogLevel.Error); return false;

            if (!bool.TryParse(split[2], out finish))
                this._monitor.LogOnce("Failed to parse bool in position 2 of network message.", LogLevel.Error); return false;

            return true;
        }
        */
    }
}