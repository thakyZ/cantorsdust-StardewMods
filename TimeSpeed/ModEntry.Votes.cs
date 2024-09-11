using System;
using System.Collections.Generic;
using StardewModdingAPI;
using TimeSpeed.Framework.Managers;

namespace TimeSpeed;
// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>The entry class called by SMAPI.</summary>
internal partial class ModEntry : Mod
{
#region Properties
    private Timer _timer { get; set; } = null!;

    /// <summary>Gets or sets whether the client voting has started</summary>
    private bool VoteStarted {
        get => this._timer.Running;
        set {
            if (value)
                this._timer.Start();

            if (!value)
                this._timer.End();
        }
    }

    /// <summary>Determines whether to show the vote status menu.</summary>
    internal static bool ShowVoteMenu
        => ModEntry._instance?.Config.FreezeTime.ClientVote == true
            && ModEntry._instance.VoteStarted;

    /// <summary>List of user votes. Keys are added and removed when a player joins or disconnects.</summary>
    private static Dictionary<long, bool?> Votes { get; } = [];
#endregion

#region Private methods
#region Event handlers
    private void OnTimerFinished(object? sender, EventArgs e)
    {
        this.TallyVotes();
        this.ResetTimer();
    }
#endregion

#region Methods
    private void ResetTimer()
    {
        foreach (long farmerID in Votes.Keys) {
            ModEntry.Votes[farmerID] = null;
        }
    }

    private void TallyVotes()
    {
        throw new NotImplementedException();
    }
#endregion

#region Internal methods
    private void PushVoteCastImpl(long farmer, bool? voteCast, bool? finish)
    {
        if (finish == true) {
            this.VoteStarted = false;
        }

        if (voteCast is not null && Votes.ContainsKey(farmer)) {
            ModEntry.Votes[farmer] = voteCast;
        }
    }

    internal static void PushVoteCast(long farmer, bool? voteCast, bool? finish)
    {
        ModEntry._instance?.PushVoteCastImpl(farmer, voteCast, finish);
    }
#endregion
#endregion
}
