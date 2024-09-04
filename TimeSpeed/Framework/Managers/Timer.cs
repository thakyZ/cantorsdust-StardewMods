using System;
using System.Collections.Generic;
using System.ComponentModel;

#nullable enable

namespace TimeSpeed.Framework.Managers
{
    internal class Timer
    {
        private static List<Timer> Timers { get; } = [];
        public bool Started { get; private set; } = false;
        public bool Running { get; private set; } = false;
        public bool Finished { get; private set; } = false;
        public bool FinishedOrStarted => this.Started || this.Finished;
        private event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? OnFinished;
        public ulong Length { get; init; }
        private ulong _counter = 0;
        private ulong Counter
        {
            get => this._counter;
            set
            {
                this._counter = value;
                this.OnPropertyChangedHandler();
            }
        }

        private void OnPropertyChangedHandler() {
            if (PropertyChanged is not null)
                PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(Timer.Counter)));
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (this.Counter >= this.Length) {
                this.End();
                this.Finished = true;
                OnFinished?.Invoke(null, new EventArgs());
            }
        }

        public Timer(byte length)
        {
            this.Length = length * 60UL * 60UL * 1_000UL;
            PropertyChanged += this.OnPropertyChanged;
            Timer.Timers.Add(this);
        }

        public void Start() {
            this.Started = true;
            this.Running = true;
        }

        public void End() {
            this.Started = false;
            this.Running = false;
        }

        public void Reset() {
            if (this.Started)
                this.End();
            this.Counter = 0;
        }

        private void IncrementTimer()
        {
            if (this.Started)
                this.Counter++;
        }

        public static void IncrementTimers()
        {
            Timers.ForEach(x => x.IncrementTimer());
        }

        public static void Dispose()
        {
            Timer.Timers.ForEach(x => x.End());
            Timer.Timers.Clear();
        }
    }
}
