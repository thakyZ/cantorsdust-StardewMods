using System;
using StardewValley;

namespace TimeSpeed.Framework;
/// <summary>Provides helper methods for tracking time flow.</summary>
internal class TimeHelper
{
#region Fields
    /// <summary>The previous tick progress.</summary>
    private double PreviousProgress;

    /// <summary>The handlers to notify when the tick progress changes.</summary>
    private event EventHandler<TickProgressChangedEventArgs>? Handlers;
#endregion


#region Accessors
    /// <summary>The game's default tick interval in milliseconds for the current location.</summary>
    public static int CurrentDefaultTickInterval => 7000 + ModEntry.BaseGameTickIntervalSeconds;

    /// <summary>The percentage of the <see cref="CurrentDefaultTickInterval"/> that's elapsed since the last tick.</summary>
    public double TickProgress
    {
        get => (double)Game1.gameTimeInterval / TimeHelper.CurrentDefaultTickInterval;
        set => Game1.gameTimeInterval = (int)(value * TimeHelper.CurrentDefaultTickInterval);
    }
#endregion


#region Public methods
    /// <summary>Update the time tracking.</summary>
    public void Update()
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator - intended
        if (this.PreviousProgress != this.TickProgress)
            this.Handlers?.Invoke(null, new TickProgressChangedEventArgs(this.PreviousProgress, this.TickProgress));

        this.PreviousProgress = this.TickProgress;
    }

    /// <summary>Register an event handler to notify when the <see cref="TickProgress"/> changes.</summary>
    /// <param name="handler">The event handler to notify.</param>
    public void WhenTickProgressChanged(EventHandler<TickProgressChangedEventArgs> handler)
    {
        this.Handlers += handler;
    }
#endregion
}
