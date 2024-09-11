namespace TimeSpeed.Framework;
/// <summary>The mod configuration for where or when time should be frozen.</summary>
internal partial class ModFreezeTimeConfig
{
#region Accessors
    /// <summary>Whether only the host can manipulate time.</summary>
    public bool HostOnly { get; set; } = true;

    /// <summary>Whether to use a voting system to allow clients to pause.</summary>
    public bool ClientVote { get; set; } = false;

    /// <summary>The threshold of how many yes votes to succeed on a client vote.</summary>
    public double VoteThreshold { get; set; } = 1.0;

    /// <summary>The length of time in minutes until the client vote passes/fails. (1~60, default: 10)</summary>
    public byte ClientVoteTimeout { get; internal set; } = 10;
#endregion
}
