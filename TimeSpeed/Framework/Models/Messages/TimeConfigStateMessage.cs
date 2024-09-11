using TimeSpeed.Framework.Models.Messages.Interfaces;

namespace TimeSpeed.Framework.Models.Messages;

internal class TimeConfigStateMessage : ITimeConfigStateMessage
{
    public long FarmerID { get; init; }
    public bool? HostOnly { get; init; } = null;
    public bool? VoteEnabled { get; init; } = null;
    public double? VoteThreshold { get; init; } = null;
}
