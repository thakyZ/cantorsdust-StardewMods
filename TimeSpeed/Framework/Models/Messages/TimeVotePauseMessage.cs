using TimeSpeed.Framework.Models.Messages.Interfaces;

namespace TimeSpeed.Framework.Models.Messages;

internal class TimeVotePauseMessage : ITimeVotePauseMessage
{
    public long FarmerID { get; init; }
    public bool? VoteCast { get; init; } = null;
    public bool? Finish { get; init; } = null;
}
