using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace TimeSpeed.Framework.Managers;

public sealed class Timer : IDisposable
{
    /// <summary>List pf instanced timers.</summary>
    private static HashSet<Timer> Timers { get; } = [];

    // ReSharper disable once RedundantDefaultMemberInitializer
    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>Determines if the timer has been started.</summary>
    public bool Started { get; private set; } = false;

    // ReSharper disable once RedundantDefaultMemberInitializer
    /// <summary>Determines if the timer is currently running.</summary>
    public bool Running { get; private set; } = false;

    // ReSharper disable once RedundantDefaultMemberInitializer
    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>Determines if the timer has finished.</summary>
    public bool Finished { get; private set; } = false;

    // ReSharper disable once UnusedMember.Global
    /// <summary>Determines if the timer has finished or started but not running.</summary>
    public bool FinishedOrStarted => this.Started || this.Finished;

    /// <summary>Fires an event when a property has changed.</summary>
    private event EventHandler<PropertyChangedEventArgs>? PropertyChanged;

    /// <summary>Fires an event when the timer has been finished.</summary>
    public event EventHandler? OnFinished;

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>The length in time of seconds that the timer lasts for.</summary>
    public ulong Length { get; }

    /// <summary>An instanced <see cref="IModHelper"/> for use with disposing and instancing.</summary>
    private static IModHelper? Helper { get; set; }

    // ReSharper disable once RedundantDefaultMemberInitializer
    /// <summary>The current count in the counter.</summary>
    private ulong _counter = 0;

    /// <summary>Determines if this timer has been disposed.</summary>
    private bool isDisposed;

    /// <summary>Gets the current second count in the timer.</summary>
    private ulong Counter
    {
        get => this._counter;
        set
        {
            this._counter = value;
            this.OnPropertyChangedHandler();
        }
    }

    /// <summary>Creates a new instance of this class.</summary>
    /// <param name="length">THe length in time in seconds.</param>
    /// <param name="helper">An instanced <see cref="IModHelper"/> class.</param>
    public Timer(byte length, IModHelper helper) {
        Timer.Helper                                 =  helper;
        helper.Events.GameLoop.OneSecondUpdateTicked += this.OnOneSecondUpdateTicked;
        this.Length                                  =  length;
        this.PropertyChanged                         += this.OnPropertyChanged;
        Timer.Timers.Add(this);
    }

    /// <summary>Method to be able to call <see cref="PropertyChangedEventHandler"/>.</summary>
    private void OnPropertyChangedHandler() {
        this.PropertyChanged?.Invoke(null, new(nameof(Timer.Counter)));
    }

    /// <summary>Fired upon a property in this class being changed.</summary>
    /// <inheritdoc cref="PropertyChangedEventHandler"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (this.Counter < this.Length) return;

        this.End();
        this.Finished = true;
        this.OnFinished?.Invoke(null, EventArgs.Empty);
    }

    /// <inheritdoc cref="IGameLoopEvents.OneSecondUpdateTicked"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e) {
        this.IncrementTimer();
    }

    /// <summary>Start the timer increment count.</summary>
    public void Start() {
        this.Started = true;
        this.Running = true;
    }

    /// <summary>End the timer from incrementing.</summary>
    public void End() {
        this.Started = false;
        this.Running = false;
    }

    // ReSharper disable once UnusedMember.Global
    /// <summary>Reset the timer by ending then reseting the counter to 0.</summary>
    public void Reset() {
        if (this.Started)
            this.End();

        this.Counter = 0;
    }

    /// <summary>Increment the tick of this client</summary>
    private void IncrementTimer()
    {
        if (this.Started)
            this.Counter++;
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    /// <param name="disposing">dispose of managed items.</param>
    [SuppressMessage("Major Code Smell", "S1172:Unused method parameters should be removed")]
    [SuppressMessage("Roslynator",       "RCS1163:Unused parameter")]
    [SuppressMessage("ReSharper",        "UnusedParameter.Local")]
    private void Dispose(bool disposing) {
        if (this.isDisposed) return;

        if (Timer.Helper is not null)
            Timer.Helper.Events.GameLoop.OneSecondUpdateTicked -= this.OnOneSecondUpdateTicked;

        this.End();
        this.isDisposed = true;
    }

    // ReSharper disable once UnusedMember.Global
    /// <summary>Dispose all timers.</summary>
    public static void DisposeAll()
    {
        foreach (Timer timer in Timer.Timers)
            timer.Dispose();

        Timer.Timers.Clear();
    }
}
