using System;
using System.Diagnostics;
using System.Reflection;
using StardewValley;
using TimeSpeed.Framework.Managers;

namespace TimeSpeed.Framework
{
    /// <summary>Displays messages to the user in-game.</summary>
    internal partial class Notifier
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Display a message for one second.</summary>
        /// <param name="message">The message to display.</param>
        /// <param name="sendToNetwork">Sends the message as a reply to the network.</param>
        public void QuickNotify(string message, bool sendToNetwork = false)
        {
            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type) { timeLeft = 1000 });
            ModEntry.IMonitor?.LogOnce($"CheckCircular: {this.CheckCircular()}");
            if (sendToNetwork)
                ModEntry.MessageManager?.SendTimeStateReply(Game1.player, message: message, timeout: 1000);
        }

        /// <summary>Display a message for one second.</summary>
        /// <param name="message">The message to display.</param>
        /// <param name="sendToNetwork">Sends the message as a reply to the network.</param>
        public void ShortNotify(string message, bool sendToNetwork = false)
        {
            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type) { timeLeft = 2000 });
            ModEntry.IMonitor?.LogOnce($"CheckCircular: {this.CheckCircular()}");
            if (sendToNetwork)
                ModEntry.MessageManager?.SendTimeStateReply(Game1.player, message: message, timeout: 2000);
        }

        /// <summary>Display a message defined by <paramref name="timeout"/> in seconds.</summary>
        /// <param name="message">The message to display.</param>
        /// <param name="timeout">The length of time in seconds to display the message.</param>
        /// <param name="sendToNetwork">Sends the message as a reply to the network.</param>
        public void Notify(string message, float timeout, bool sendToNetwork = false)
        {
            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type) { timeLeft = timeout });
            ModEntry.IMonitor?.LogOnce($"CheckCircular: {this.CheckCircular()}");
            if (sendToNetwork)
                ModEntry.MessageManager?.SendTimeStateReply(Game1.player, message: message, timeout: timeout);
        }

        private bool CheckCircular() {
            var stackTrace = new StackTrace();
            return !Array.Exists(stackTrace.GetFrames(), (StackFrame stackFrame)
                => stackFrame.HasMethod()
                && stackFrame.GetMethod() is MethodBase methodBase
                && methodBase.DeclaringType == typeof(MessageManager)
                && (
                    (methodBase.Name.ToLower().StartsWith("process") && methodBase.Name.ToLower().EndsWith("message"))
                    || (methodBase.Name.ToLower().StartsWith("send") && methodBase.Name.ToLower().EndsWith("message"))
                )
            );
        }
    }
}
