using StardewValley;

namespace TimeSpeed.Framework
{
    /// <summary>Displays messages to the user in-game.</summary>
    internal static class Notifier
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Display a message for one second.</summary>
        /// <param name="message">The message to display.</param>
        public static void QuickNotify(string message)
        {
            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type) { timeLeft = 1000 });
            ModEntry.MessageManager?.SendTimeStateReply(Game1.player, message: message, timeout: 1000);
        }

        /// <summary>Display a message for two seconds.</summary>
        /// <param name="message">The message to display.</param>
        public static void ShortNotify(string message)
        {
            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type) { timeLeft = 2000 });
            ModEntry.MessageManager?.SendTimeStateReply(Game1.player, message: message, timeout: 2000);
        }
    }
}
