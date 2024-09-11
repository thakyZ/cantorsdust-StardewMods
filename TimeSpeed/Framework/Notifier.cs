using StardewValley;

namespace TimeSpeed.Framework;
/// <summary>Displays messages to the user in-game.</summary>
internal partial class Notifier
{
#region Public methods
    // ReSharper disable once UnusedMember.Global
    /// <summary>Display a message for one second.</summary>
    /// <param name="message">The message to display.</param>
    public void QuickNotify(string message)
    {
        // ReSharper disable once ArrangeObjectCreationWhenTypeNotEvident
        Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type) { timeLeft = 1000 });
    }

    // ReSharper disable once MemberCanBeMadeStatic.Global
    /// <summary>Display a message for two seconds.</summary>
    /// <param name="message">The message to display.</param>
    public void ShortNotify(string message)
    {
        // ReSharper disable once ArrangeObjectCreationWhenTypeNotEvident
        Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type) { timeLeft = 2000 });
    }
#endregion
}
